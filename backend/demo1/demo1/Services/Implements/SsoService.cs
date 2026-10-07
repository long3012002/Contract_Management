using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using demo1.Data;
using demo1.DTOs;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    /// <summary>
    /// Service độc lập phụ trách toàn bộ nghiệp vụ xác thực SSO (OAuth2 / OIDC Authorization Code Flow + PKCE).
    /// </summary>
    public class SsoService : ISsoService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SsoService> _logger;
        private readonly IWebHostEnvironment? _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public SsoService(
            IConfiguration configuration,
            AppDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SsoService> logger,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment? env = null)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _env = env;
        }

        public async Task<AuthResult> HandleCallbackAsync(SsoCallbackRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.CodeVerifier))
                {
                    return AuthResult.Fail(400, "Mã xác thực (code) và code_verifier không được để trống.");
                }

                var tokenUrl = _configuration["SSO:TokenUrl"] ?? "http://103.143.207.168:32081/api/v1/oauth2/token";
                var clientId = _configuration["SSO:ClientId"] ?? "qlcv";
                var clientSecret = _configuration["SSO:ClientSecret"] ?? "FLaMbsZ2r0qdcSWytHcgHqfR44vgcXs8";

                // 1. Trao đổi Authorization Code + PKCE code_verifier lấy Token từ SSO Server
                var httpClient = _httpClientFactory.CreateClient("SsoClient");
                var tokenParams = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", request.Code },
                    { "redirect_uri", request.RedirectUri },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "code_verifier", request.CodeVerifier }
                };

                var tokenResponseMsg = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(tokenParams));
                if (!tokenResponseMsg.IsSuccessStatusCode)
                {
                    var errorBody = await tokenResponseMsg.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi trao đổi SSO Token endpoint: {Status} - {Error}", tokenResponseMsg.StatusCode, errorBody);
                    return AuthResult.Fail(400, "Xác thực mã đăng nhập SSO không thành công hoặc mã đã hết hạn.");
                }

                var tokenJson = await tokenResponseMsg.Content.ReadAsStringAsync();
                var ssoTokens = JsonSerializer.Deserialize<SsoTokenResponse>(tokenJson);

                if (ssoTokens == null || string.IsNullOrWhiteSpace(ssoTokens.IdToken))
                {
                    return AuthResult.Fail(400, "Không nhận được ID Token từ hệ thống SSO.");
                }

                // 2. Phân tích ID Token lấy Subject ID ('sub') và Email / Preferred Username
                var handler = new JwtSecurityTokenHandler();
                string? sub = null;
                string? email = null;
                string? preferredUsername = null;

                if (!string.IsNullOrWhiteSpace(ssoTokens.IdToken) && handler.CanReadToken(ssoTokens.IdToken))
                {
                    var jwtToken = handler.ReadJwtToken(ssoTokens.IdToken);
                    sub = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == "sub")?.Value;
                    email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == "email")?.Value;
                    preferredUsername = jwtToken.Claims.FirstOrDefault(c => c.Type == "preferred_username" || c.Type == "username")?.Value;
                }

                // Fallback: Nếu ID Token không có đủ 'sub' hoặc thông tin người dùng, gọi sang UserInfo Endpoint của SSO
                if (string.IsNullOrWhiteSpace(sub) && !string.IsNullOrWhiteSpace(ssoTokens.AccessToken))
                {
                    try
                    {
                        var userInfoUrl = _configuration["SSO:UserInfoUrl"] ?? "http://103.143.207.168:32081/api/v1/oauth2/userinfo";
                        using var userInfoReq = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
                        userInfoReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ssoTokens.AccessToken);

                        var userInfoResp = await httpClient.SendAsync(userInfoReq);
                        if (userInfoResp.IsSuccessStatusCode)
                        {
                            var userInfoJson = await userInfoResp.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(userInfoJson);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("sub", out var subProp)) sub = subProp.GetString();
                            if (string.IsNullOrWhiteSpace(email) && root.TryGetProperty("email", out var emailProp)) email = emailProp.GetString();
                            if (string.IsNullOrWhiteSpace(preferredUsername) && root.TryGetProperty("preferred_username", out var prefProp)) preferredUsername = prefProp.GetString();
                            if (string.IsNullOrWhiteSpace(preferredUsername) && root.TryGetProperty("username", out var uProp)) preferredUsername = uProp.GetString();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể lấy thông tin từ UserInfo Endpoint, sử dụng thông tin từ ID Token.");
                    }
                }

                if (string.IsNullOrWhiteSpace(sub))
                {
                    return AuthResult.Fail(400, "Không trích xuất được định danh cố định (claim 'sub') từ ID Token hoặc UserInfo Endpoint của SSO.");
                }

                // 3. Tra cứu người dùng trong cơ sở dữ liệu hệ thống
                // Ưu tiên 1: Tra cứu theo SsoSub đã được liên kết trước đó (Khóa duy nhất cố định theo tài liệu SSO)
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SsoSub == sub);


                // Ưu tiên 2 (Lần đầu đăng nhập): Liên kết theo Email hoặc Username
                if (user == null)
                {
                    user = await _dbContext.Users.FirstOrDefaultAsync(u =>
                        (!string.IsNullOrEmpty(email) && u.Email == email) ||
                        (!string.IsNullOrEmpty(preferredUsername) && u.Username == preferredUsername));

                    if (user != null)
                    {
                        user.SsoSub = sub;
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Đã liên kết thành công tài khoản '{Username}' với SSO Sub '{Sub}'", user.Username, sub);
                    }
                }

                if (user == null)
                {
                    var accountName = email ?? preferredUsername ?? sub;
                    await LogAuthEventAsync(accountName, "SSO_LOGIN_FAILED", $"Tài khoản SSO ({accountName}) chưa được phân quyền trong hệ thống.");
                    return AuthResult.Fail(403, $"Tài khoản SSO ({accountName}) chưa được cấp quyền sử dụng hệ thống Quản lý Dự án.");
                }

                if (!user.IsActive)
                {
                    await LogAuthEventAsync(user.Username, "SSO_LOGIN_FAILED", "Tài khoản đang bị khóa hoặc ngưng hoạt động.", user.Id.ToString());
                    return AuthResult.Fail(403, "Tài khoản của bạn đang bị khóa hoặc ngưng hoạt động.");
                }

                // 4. Phát hành JWT Access Token và Refresh Token nội bộ của ứng dụng
                var accessToken = GenerateJwtToken(user.Username, 180, false, user.Id);
                var refreshToken = GenerateJwtToken(user.Username, 10080, false, user.Id);

                user.RefreshTokenHash = ComputeHash(refreshToken);
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
                await _dbContext.SaveChangesAsync();

                // Lưu cookies HttpOnly
                SetAuthCookies(accessToken, refreshToken, 180, 10080);

                await LogAuthEventAsync(user.Username, "SSO_LOGIN_SUCCESS", "Đăng nhập thành công qua SSO", user.Id.ToString());

                return AuthResult.Success(new LoginResponse
                {
                    Message = "Đăng nhập SSO thành công",
                    UserId = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    IsSystemAdmin = user.IsSystemAdmin,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    Permissions = await GetEffectivePermissionsAsync(user.Id)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý SSO Callback.");
                return AuthResult.Fail(500, "Đã có lỗi xảy ra trong quá trình xác thực với hệ thống SSO.");
            }
        }

        private async Task<List<RolePermissionDto>> GetEffectivePermissionsAsync(Guid userId)
        {
            var resultDict = new Dictionary<string, RolePermissionDto>(StringComparer.OrdinalIgnoreCase);

            var roleIds = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (roleIds.Count > 0)
            {
                var rolePerms = await _dbContext.RolePermissions
                    .AsNoTracking()
                    .Include(rp => rp.Feature)
                    .Where(rp => roleIds.Contains(rp.RoleId) && rp.Feature != null)
                    .ToListAsync();

                foreach (var g in rolePerms.GroupBy(rp => rp.FeatureId))
                {
                    var feature = g.First().Feature!;
                    var canAccess = g.Any(rp => rp.CanAccess);
                    if (!canAccess) continue;

                    var allPerms = g
                        .Where(rp => !string.IsNullOrWhiteSpace(rp.Permissions))
                        .SelectMany(rp => rp.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        .Select(p => p.ToUpper())
                        .Distinct()
                        .ToList();

                    if (allPerms.Count == 0) continue;

                    resultDict[feature.Code] = new RolePermissionDto
                    {
                        FeatureId = feature.Id,
                        FeatureCode = feature.Code,
                        FeatureName = feature.Name,
                        CanAccess = true,
                        Permissions = string.Join(',', allPerms)
                    };
                }
            }

            var userPerms = await _dbContext.UserPermissions
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up => up.UserId == userId)
                .ToListAsync();

            var allFeatures = await _dbContext.Features.AsNoTracking().ToListAsync();
            var featureMap = allFeatures.ToDictionary(f => f.Code, StringComparer.OrdinalIgnoreCase);

            foreach (var up in userPerms)
            {
                var featCode = up.FeatureCode;
                if (string.IsNullOrWhiteSpace(featCode)) continue;

                var permCode = up.Permission?.Code?.ToUpper();
                if (string.IsNullOrWhiteSpace(permCode)) continue;

                if (resultDict.TryGetValue(featCode, out var existing))
                {
                    var currentList = (existing.Permissions ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(p => p.ToUpper())
                        .ToList();

                    if (!currentList.Contains(permCode))
                    {
                        currentList.Add(permCode);
                        existing.Permissions = string.Join(',', currentList);
                    }
                }
                else
                {
                    featureMap.TryGetValue(featCode, out var featEntity);
                    resultDict[featCode] = new RolePermissionDto
                    {
                        FeatureId = featEntity?.Id ?? Guid.Empty,
                        FeatureCode = featCode,
                        FeatureName = featEntity?.Name ?? featCode,
                        CanAccess = true,
                        Permissions = permCode
                    };
                }
            }

            return resultDict.Values.OrderBy(dto => dto.FeatureCode).ToList();
        }

        private string GenerateJwtToken(string username, double expiryInMinutes, bool isTemp = false, Guid? userId = null)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "Iip7U9SQ3R8wZdAaicLRbrJKBeG8zgEYeX6wlfw8p7k=";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(expiryInMinutes);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }

            if (isTemp)
            {
                claims.Add(new Claim("is_temp", "true"));
            }

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"] ?? "ContractManagementBackend",
                audience: jwtSettings["Audience"] ?? "ContractManagementFrontend",
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private void SetAuthCookies(string accessToken, string? refreshToken, double accessExpiryInMinutes, double refreshExpiryInMinutes)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            bool isHttps = httpContext.Request.IsHttps ||
                string.Equals(httpContext.Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = isHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            cookieOptions.Expires = DateTime.UtcNow.AddMinutes(accessExpiryInMinutes);
            httpContext.Response.Cookies.Append("access_token", accessToken, cookieOptions);

            if (!string.IsNullOrEmpty(refreshToken))
            {
                cookieOptions.Expires = DateTime.UtcNow.AddMinutes(refreshExpiryInMinutes);
                httpContext.Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
            }
        }

        private async Task LogAuthEventAsync(string username, string action, string details, string? userId = null)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                var ip = context?.Connection?.RemoteIpAddress?.ToString();

                var auditLog = new demo1.Entity.AuditLog
                {
                    UserId = userId,
                    Username = username,
                    Action = action,
                    TableName = "Users",
                    EntityId = username,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ip,
                    NewValues = details
                };

                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[SsoEvent] {Action} | User: '{Username}' | IP: {Ip} | Details: {Details}", action, username, ip, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi ghi audit log cho sự kiện SSO: {Username}", username);
            }
        }
    }
}
