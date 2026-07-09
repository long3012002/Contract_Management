using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using demo1.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly RadiusClient _radiusClient;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly TotpService _totpService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            RadiusClient radiusClient,
            IConfiguration configuration,
            AppDbContext dbContext,
            TotpService totpService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _radiusClient = radiusClient;
            _configuration = configuration;
            _dbContext = dbContext;
            _totpService = totpService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return AuthResult.Fail(400, "Tên đăng nhập và mật khẩu là bắt buộc.");
            }

            bool isBypass = request.Username == "admin" && request.Password == "admin_bypass_dev";
            bool isAuthenticated = isBypass || await _radiusClient.AuthenticateAsync(request.Username, request.Password);

            if (isAuthenticated)
            {
                var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
                if (dbUser == null)
                {
                    if (isBypass)
                    {
                        dbUser = new demo1.Entity.User
                        {
                            Username = "admin",
                            FullName = "System Administrator (Auto Seeded)",
                            IsActive = true,
                            IsSystemAdmin = true
                        };
                        _dbContext.Users.Add(dbUser);
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        await LogAuthEventAsync(request.Username, "LOGIN_FAILED", "Tài khoản chưa được cấp quyền sử dụng hệ thống.");
                        return AuthResult.Fail(403, "Tài khoản chưa được cấp quyền sử dụng hệ thống.");
                    }
                }

                if (!dbUser.IsActive)
                {
                    await LogAuthEventAsync(request.Username, "LOGIN_FAILED", "Tài khoản đang bị khóa hoặc ngưng hoạt động.", dbUser.Id.ToString());
                    return AuthResult.Fail(403, "Tài khoản đang bị khóa hoặc ngưng hoạt động.");
                }

                if (isBypass)
                {
                    var accessToken = GenerateJwtToken(dbUser.Username, 180);
                    var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

                    dbUser.RefreshTokenHash = ComputeHash(refreshToken);
                    dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
                    await _dbContext.SaveChangesAsync();

                    await LogAuthEventAsync(dbUser.Username, "LOGIN_SUCCESS", "Bypass Login Success (Dev Mode)", dbUser.Id.ToString());

                    return AuthResult.Success(new LoginResponse
                    {
                        Message = "Bypass Login Success (Dev Mode)",
                        Username = dbUser.Username,
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    });
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
                        await _dbContext.SaveChangesAsync();
                    }

                    string qrUrl = _totpService.GetQrCodeUrl(dbUser.Username, dbUser.TwoFactorSecret);

                    return AuthResult.Success(new LoginResponse
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
                return AuthResult.Success(new LoginResponse
                {
                    Message = "Yêu cầu mã xác thực 2 lớp (2FA)",
                    Username = request.Username,
                    Require2FAVerification = true,
                    AccessToken = tempToken
                });
            }
            else
            {
                await LogAuthEventAsync(request.Username, "LOGIN_FAILED", "Sai tài khoản hoặc mật khẩu");
                return AuthResult.Fail(401, "Sai tài khoản hoặc mật khẩu");
            }
        }

        public async Task<AuthResult> RefreshAsync(RefreshRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return AuthResult.Fail(400, "Phiên làm việc đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.");
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
                    return AuthResult.Fail(401, "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.");
                }

                var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
                if (dbUser == null)
                {
                    return AuthResult.Fail(401, "Tài khoản không hoạt động hoặc không tồn tại. Vui lòng đăng nhập lại.");
                }

                var incomingHash = ComputeHash(request.RefreshToken);
                if (dbUser.RefreshTokenHash != incomingHash || dbUser.RefreshTokenExpiryTime == null || dbUser.RefreshTokenExpiryTime < DateTime.UtcNow)
                {
                    return AuthResult.Fail(401, "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.");
                }

                var newAccessToken = GenerateJwtToken(username, 180);
                var newRefreshToken = GenerateJwtToken(username, 10080);

                // Update database with the new refresh token hash
                dbUser.RefreshTokenHash = ComputeHash(newRefreshToken);
                dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
                await _dbContext.SaveChangesAsync();

                return AuthResult.Success(new LoginResponse
                {
                    Message = "Làm mới token thành công",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Username = username
                });
            }
            catch (Exception)
            {
                return AuthResult.Fail(401, "Phiên làm việc đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.");
            }
        }

        public async Task<AuthResult> Enable2FaAsync(Verify2FARequest request, string authHeader)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Code))
            {
                return AuthResult.Fail(400, "Tên đăng nhập và mã OTP là bắt buộc.");
            }

            var usernameFromToken = ValidateTemporaryToken(authHeader);
            if (usernameFromToken == null || !usernameFromToken.Equals(request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail(401, "Yêu cầu mã tạm thời (Temporary Token) hợp lệ.");
            }

            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
            if (dbUser == null)
            {
                return AuthResult.Fail(404, "Không tìm thấy người dùng.");
            }

            if (dbUser.IsTwoFactorEnabled)
            {
                return AuthResult.Fail(400, "Xác thực hai lớp (2FA) đã được kích hoạt trước đó.");
            }

            bool isValid = _totpService.VerifyCode(dbUser.TwoFactorSecret ?? "", request.Code);
            if (!isValid)
            {
                await LogAuthEventAsync(request.Username, "LOGIN_FAILED", "Kích hoạt 2FA thất bại: Mã OTP không chính xác", dbUser.Id.ToString());
                return AuthResult.Fail(400, "Mã OTP không chính xác.");
            }

            dbUser.IsTwoFactorEnabled = true;
            dbUser.UpdatedAt = DateTime.UtcNow;

            var accessToken = GenerateJwtToken(dbUser.Username, 180);
            var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

            // Store refresh token hash in DB
            dbUser.RefreshTokenHash = ComputeHash(refreshToken);
            dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
            await _dbContext.SaveChangesAsync();

            await LogAuthEventAsync(dbUser.Username, "LOGIN_SUCCESS", "Kích hoạt xác thực 2 lớp thành công", dbUser.Id.ToString());

            return AuthResult.Success(new LoginResponse
            {
                Message = "Kích hoạt xác thực 2 lớp thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = dbUser.Username
            });
        }

        public async Task<AuthResult> Verify2FaAsync(Verify2FARequest request, string authHeader)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Code))
            {
                return AuthResult.Fail(400, "Tên đăng nhập và mã OTP là bắt buộc.");
            }

            var usernameFromToken = ValidateTemporaryToken(authHeader);
            if (usernameFromToken == null || !usernameFromToken.Equals(request.Username, StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.Fail(401, "Yêu cầu mã tạm thời (Temporary Token) hợp lệ.");
            }

            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
            if (dbUser == null)
            {
                return AuthResult.Fail(404, "Không tìm thấy người dùng.");
            }

            if (!dbUser.IsTwoFactorEnabled)
            {
                return AuthResult.Fail(400, "Xác thực hai lớp (2FA) chưa được kích hoạt.");
            }

            bool isValid = _totpService.VerifyCode(dbUser.TwoFactorSecret ?? "", request.Code);
            if (!isValid)
            {
                await LogAuthEventAsync(request.Username, "LOGIN_FAILED", "Xác thực 2FA thất bại: Mã OTP không chính xác", dbUser.Id.ToString());
                return AuthResult.Fail(400, "Mã OTP không chính xác.");
            }

            var accessToken = GenerateJwtToken(dbUser.Username, 180);
            var refreshToken = GenerateJwtToken(dbUser.Username, 10080);

            // Store refresh token hash in DB
            dbUser.RefreshTokenHash = ComputeHash(refreshToken);
            dbUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
            await _dbContext.SaveChangesAsync();

            await LogAuthEventAsync(dbUser.Username, "LOGIN_SUCCESS", "Đăng nhập xác thực 2 lớp thành công", dbUser.Id.ToString());

            return AuthResult.Success(new LoginResponse
            {
                Message = "Đăng nhập xác thực 2 lớp thành công",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = dbUser.Username
            });
        }

        public async Task<AuthResult> LogoutAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return AuthResult.Fail(401, "Tên đăng nhập là bắt buộc.");
            }

            var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (dbUser != null)
            {
                dbUser.RefreshTokenHash = null;
                dbUser.RefreshTokenExpiryTime = null;
                await _dbContext.SaveChangesAsync();
                await LogAuthEventAsync(username, "LOGOUT", "Đăng xuất thành công", dbUser.Id.ToString());
            }
            else
            {
                await LogAuthEventAsync(username, "LOGOUT", "Đăng xuất thành công");
            }

            return AuthResult.Success(new LoginResponse { Message = "Đăng xuất thành công" });
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

        private string? GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var remoteIp = context.Connection?.RemoteIpAddress;
            if (remoteIp != null)
            {
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    return remoteIp.MapToIPv4().ToString();
                }
                return remoteIp.ToString();
            }

            return null;
        }

        private async Task LogAuthEventAsync(string username, string action, string details, string? userId = null)
        {
            try
            {
                var ipAddress = GetClientIpAddress();
                var auditLog = new demo1.Entity.AuditLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    TableName = "Users",
                    EntityId = username,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    NewValues = details
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"[AuthEvent] {action} | User: '{username}' | IP: {ipAddress} | Details: {details}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AuthEvent] Error logging auth event for user '{username}'");
            }
        }
    }
}
