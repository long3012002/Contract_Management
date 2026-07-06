using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
                // Sync user with local database
                var dbUser = _dbContext.Users.FirstOrDefault(u => u.Username == request.Username);
                if (dbUser == null)
                {
                    dbUser = new User
                    {
                        Username = request.Username,
                        FullName = request.Username, // Default to username as name
                        IsActive = true,
                        IsSystemAdmin = request.Username.ToLower() == "quangmd" || request.Username.ToLower() == "admin",
                        IsTwoFactorEnabled = false
                    };
                    _dbContext.Users.Add(dbUser);
                    _dbContext.SaveChanges();
                }

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
                        TwoFactorSecret = dbUser.TwoFactorSecret,
                        QrCodeUrl = qrUrl
                    });
                }

                // If 2FA is enabled, require code verification
                return Ok(new LoginResponse
                {
                    Message = "Yêu cầu mã xác thực 2 lớp (2FA)",
                    Username = request.Username,
                    Require2FAVerification = true
                });
            }
            else
            {
                return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
            }
        }

        //[HttpPost("test")]
        //[ProducesResponseType(typeof(LoginResponse), 200)]
        //public async Task<IActionResult> TestLogin()
        //{
        //    bool result = await _radiusClient.AuthenticateAsync("quangmd", "XianWang072026");

        //    if (result)
        //    {
        //        var accessToken = GenerateJwtToken("quangmd", 180);
        //        var refreshToken = GenerateJwtToken("quangmd", 10080);
        //        return Ok(new LoginResponse 
        //        { 
        //            Message = "Đăng nhập thành công", 
        //            AccessToken = accessToken, 
        //            RefreshToken = refreshToken, 
        //            Username = "quangmd" 
        //        });
        //    }
        //    else
        //    {
        //        return Unauthorized(new { Message = "Sai tài khoản hoặc mật khẩu" });
        //    }
        //}

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

                var newAccessToken = GenerateJwtToken(username, 180);
                var newRefreshToken = GenerateJwtToken(username, 10080);

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
            _dbContext.SaveChanges();

            var accessToken = GenerateJwtToken(dbUser.Username, 180);
            var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

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

            return Ok(new LoginResponse
            {
                Message = "Đăng nhập xác thực 2 lớp thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = dbUser.Username
            });
        }

        private string GenerateJwtToken(string username, double expiryInMinutes)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var issuer = jwtSettings["Issuer"] ?? "ContractManagementBackend";
            var audience = jwtSettings["Audience"] ?? "ContractManagementFrontend";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, username)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
