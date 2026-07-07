using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using demo1.DTOs;
using demo1.Services;
using demo1.Entity;
using demo1.Data;

namespace demo1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RadiusClient _radiusClient;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly TotpService _totpService;

        public AuthController(RadiusClient radiusClient, IConfiguration configuration, AppDbContext dbContext, TotpService totpService)
        {
            _radiusClient = radiusClient;
            _configuration = configuration;
            _dbContext = dbContext;
            _totpService = totpService;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Username and password are required." });
            }

            bool isAuthenticated = await _radiusClient.AuthenticateAsync(request.Username, request.Password);

            if (isAuthenticated)
            {
                var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == request.Username);
                if (dbUser == null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Tài khoản chưa được cấp quyền sử dụng hệ thống." });
                }

                if (!dbUser.IsActive)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Tài khoản đang bị khóa hoặc ngưng hoạt động." });
                }

                // Generate short-lived Temporary Token (3 minutes) for 2FA validation
                var tempToken = GenerateJwtToken(request.Username, 3, isTemp: true);

                // If user is not system admin (or system admin too - we require 2FA for all users as requested)
                // Check if Google Authenticator 2FA is enabled
                if (!dbUser.IsTwoFactorEnabled)
                {
                    // Generate new secret if not set yet
                    if (string.IsNullOrEmpty(dbUser.TwoFactorSecret))
                    {
                        dbUser.TwoFactorSecret = _totpService.GenerateSecret();
                        _dbContext.SaveChanges();
                    }

                    string qrUrl = _totpService.GetQrCodeUrl(dbUser.Username, dbUser.TwoFactorSecret);

                    return Ok(new LoginResponse
                    {
                        Message = "Yêu cầu bật xác thực 2 lớp (lần đầu đăng nhập)",
                        Username = request.Username,
                        Require2FASetup = true,
                        TwoFactorSecret = null, // Do not expose raw secret key in login response
                        QrCodeUrl = qrUrl,
                        AccessToken = tempToken
                    });
                }

                // If 2FA is enabled, require code verification
                return Ok(new LoginResponse
                {
                    Message = "Yêu cầu mã xác thực 2 lớp (2FA)",
                    Username = request.Username,
                    Require2FAVerification = true,
                    AccessToken = tempToken
                });
            }
            else
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { Message = "Refresh token is required." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "ContractManagementBackend",
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"] ?? "ContractManagementFrontend",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(request.RefreshToken, validationParameters, out var validatedToken);
                var username = principal.Identity?.Name ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized(new { Message = "Invalid token payload." });
                }

                var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == username && u.IsActive);
                if (dbUser == null)
                {
                    return Unauthorized(new { Message = "User is inactive or not found." });
                }

                var incomingHash = ComputeHash(request.RefreshToken);
                if (dbUser.RefreshTokenHash != incomingHash || dbUser.RefreshTokenExpiryTime == null || dbUser.RefreshTokenExpiryTime < DateTime.UtcNow)
                {
                    return Unauthorized(new { Message = "Refresh token has been revoked or expired." });
                }

                var newAccessToken = GenerateJwtToken(username, 180);
                var newRefreshToken = GenerateJwtToken(username, 10080);

                // Update database with the new refresh token hash
                dbUser.RefreshTokenHash = ComputeHash(newRefreshToken);
                dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
                _dbContext.SaveChanges();

                return Ok(new LoginResponse
                {
                    Message = "Làm mới token thành công",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Username = username
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = "Invalid or expired refresh token.", Error = ex.Message });
            }
        }

        [HttpPost("enable-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Enable2Fa([FromBody] Verify2FARequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { Message = "Username and code are required." });
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            var usernameFromToken = ValidateTemporaryToken(authHeader);
            if (usernameFromToken == null || !usernameFromToken.Equals(request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { Message = "Yêu cầu mã tạm thời (Temporary Token) hợp lệ." });
            }

            var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == request.Username && u.IsActive);
            if (dbUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (dbUser.IsTwoFactorEnabled)
            {
                return BadRequest(new { Message = "2FA is already enabled." });
            }

            bool isValid = _totpService.VerifyCode(dbUser.TwoFactorSecret ?? "", request.Code);
            if (!isValid)
            {
                return BadRequest(new { Message = "Mã OTP không chính xác." });
            }

            dbUser.IsTwoFactorEnabled = true;
            dbUser.UpdatedAt = DateTime.UtcNow;

            var accessToken = GenerateJwtToken(dbUser.Username, 180);
            var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

            // Store refresh token hash in DB
            dbUser.RefreshTokenHash = ComputeHash(refreshToken);
            dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
            _dbContext.SaveChanges();

            return Ok(new LoginResponse
            {
                Message = "Kích hoạt xác thực 2 lớp thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = dbUser.Username
            });
        }

        [HttpPost("verify-2fa")]
        [ProducesResponseType(typeof(LoginResponse), 200)]
        public async Task<IActionResult> Verify2Fa([FromBody] Verify2FARequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { Message = "Username and code are required." });
            }

            var authHeader = Request.Headers["Authorization"].ToString();
            var usernameFromToken = ValidateTemporaryToken(authHeader);
            if (usernameFromToken == null || !usernameFromToken.Equals(request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { Message = "Yêu cầu mã tạm thời (Temporary Token) hợp lệ." });
            }

            var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == request.Username && u.IsActive);
            if (dbUser == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (!dbUser.IsTwoFactorEnabled)
            {
                return BadRequest(new { Message = "2FA is not enabled." });
            }

            bool isValid = _totpService.VerifyCode(dbUser.TwoFactorSecret ?? "", request.Code);
            if (!isValid)
            {
                return BadRequest(new { Message = "Mã OTP không chính xác." });
            }

            var accessToken = GenerateJwtToken(dbUser.Username, 180);
            var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

            // Store refresh token hash in DB
            dbUser.RefreshTokenHash = ComputeHash(refreshToken);
            dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
            _dbContext.SaveChanges();

            return Ok(new LoginResponse
            {
                Message = "Đăng nhập xác thực 2 lớp thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = dbUser.Username
            });
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == username);
            if (dbUser != null)
            {
                dbUser.RefreshTokenHash = null;
                dbUser.RefreshTokenExpiryTime = null;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { Message = "Đăng xuất thành công" });
        }

        private string GenerateJwtToken(string username, double expiryInMinutes, bool isTemp = false)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (isTemp || expiryInMinutes <= 5)
            {
                claims.Add(new Claim("is_temp", "true"));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = jwtSettings["Issuer"] ?? "ContractManagementBackend",
                Audience = jwtSettings["Audience"] ?? "ContractManagementFrontend",
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string? ValidateTemporaryToken(string authorizationHeader)
        {
            Console.WriteLine($"[ValidateTemporaryToken] Received Header: '{authorizationHeader}'");
            if (string.IsNullOrWhiteSpace(authorizationHeader))
            {
                Console.WriteLine("[ValidateTemporaryToken Error]: Authorization header is empty or null.");
                return null;
            }
            if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[ValidateTemporaryToken Error]: Authorization header does not start with 'Bearer '.");
                return null;
            }

            var token = authorizationHeader.Substring(7);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var jwtToken = validatedToken as JwtSecurityToken;

                if (jwtToken == null || jwtToken.Payload.TryGetValue("is_temp", out var isTemp) && isTemp.ToString() != "true")
                {
                    return null;
                }

                return principal.Identity?.Name ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? jwtToken.Subject;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ValidateTemporaryToken Error]: {ex.Message}");
                return null;
            }
        }

        private string ComputeHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
