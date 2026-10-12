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
using Microsoft.Extensions.Caching.Memory;
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
    /// Tuân thủ tiêu chuẩn kỹ thuật tài liệu Hướng dẫn tích hợp SSO v2.1.
    /// </summary>
    public class SsoService : ISsoService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SsoService> _logger;
        private readonly IWebHostEnvironment? _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache? _cache;

        public SsoService(
            IConfiguration configuration,
            AppDbContext dbContext,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SsoService> logger,
            IHttpClientFactory httpClientFactory,
            IMemoryCache? cache = null,
            IWebHostEnvironment? env = null)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _env = env;
        }

        public async Task<AuthResult> HandleCallbackAsync(SsoCallbackRequest request)
        {
            try
            {
                // 1. Kiểm tra tính toàn vẹn của tham số Callback (bắt buộc có code, code_verifier và state chống CSRF)
                if (request == null || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.CodeVerifier) || string.IsNullOrWhiteSpace(request.State))
                {
                    return AuthResult.Fail(400, "Mã xác thực (code), code_verifier và state không được để trống.");
                }

                var tokenUrl = _configuration["SSO:TokenUrl"] ?? "http://103.143.207.168:32081/api/v1/oauth2/token";
                var clientId = _configuration["SSO:ClientId"] ?? "qlcv";
                var clientSecret = _configuration["SSO:ClientSecret"] ?? "FLaMbsZ2r0qdcSWytHcgHqfR44vgcXs8";

                // 2. Trao đổi Authorization Code + PKCE code_verifier lấy Token từ SSO Server (phương thức client_secret_post theo mục A.3)
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

                // 3. Xác minh chữ ký số ID Token bằng JWKS và bóc tách các Claims định danh (sub, email, preferred_username)
                var tokenValidation = await ValidateIdTokenAsync(ssoTokens.IdToken, httpClient);
                if (!tokenValidation.IsValid)
                {
                    _logger.LogWarning("Xác thực ID Token không thành công: {Error}", tokenValidation.ErrorMessage);
                    return AuthResult.Fail(400, tokenValidation.ErrorMessage ?? "Xác thực chữ ký ID Token không thành công.");
                }

                string? sub = tokenValidation.Sub;
                string? email = tokenValidation.Email;
                string? preferredUsername = tokenValidation.PreferredUsername;

                // 4. Fallback: Nếu ID Token thiếu thông tin hoặc cấu hình PA2 cần lấy thêm danh sách quyền
                if ((string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(preferredUsername)) && !string.IsNullOrWhiteSpace(ssoTokens.AccessToken))
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
                            var userInfo = JsonSerializer.Deserialize<SsoUserInfoResponse>(userInfoJson);
                            if (userInfo != null)
                            {
                                if (string.IsNullOrWhiteSpace(sub)) sub = userInfo.Sub ?? userInfo.Id;
                                if (string.IsNullOrWhiteSpace(email)) email = userInfo.Email ?? userInfo.Attributes?.EmailAddress;
                                if (string.IsNullOrWhiteSpace(preferredUsername)) preferredUsername = userInfo.PreferredUsername ?? userInfo.Username;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể lấy thông tin từ UserInfo Endpoint, tiếp tục với dữ liệu từ ID Token.");
                    }
                }

                if (string.IsNullOrWhiteSpace(sub))
                {
                    return AuthResult.Fail(400, "Không trích xuất được định danh cố định (claim 'sub') từ ID Token hoặc UserInfo Endpoint của SSO.");
                }

                // 5. Tra cứu người dùng trong cơ sở dữ liệu hệ thống
                // Ưu tiên 1: Tra cứu theo SsoSub đã được liên kết trước đó (Khóa định danh bất biến theo mục A.5.2)
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.SsoSub == sub);

                // Ưu tiên 2 (Lần đầu đăng nhập): Tự động liên kết theo Email hoặc Username
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

                // 6. Phát hành JWT Access Token và Refresh Token nội bộ của ứng dụng
                var accessToken = GenerateJwtToken(user.Username, 180, false, user.Id);
                var refreshToken = GenerateJwtToken(user.Username, 10080, false, user.Id);

                user.RefreshTokenHash = ComputeHash(refreshToken);
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(10080);
                await _dbContext.SaveChangesAsync();

                // Lưu cookies HttpOnly an toàn (mục 4.5 & 8.1)
                SetAuthCookies(accessToken, refreshToken, 180, 10080);

                await LogAuthEventAsync(user.Username, "SSO_LOGIN_SUCCESS", "Đăng nhập thành công qua SSO OIDC PKCE", user.Id.ToString());

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

        private async Task<(bool IsValid, string? Sub, string? Email, string? PreferredUsername, string? ErrorMessage)> ValidateIdTokenAsync(string idToken, HttpClient httpClient)
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(idToken))
            {
                return (false, null, null, null, "Định dạng ID Token không hợp lệ.");
            }

            var validateSignature = _configuration.GetValue<bool>("SSO:ValidateTokenSignature", true);
            if (!validateSignature)
            {
                var jwt = handler.ReadJwtToken(idToken);
                var subVal = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == "sub")?.Value;
                var emailVal = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == "email")?.Value;
                var prefVal = jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username" || c.Type == "username")?.Value;
                return (true, subVal, emailVal, prefVal, null);
            }

            var jwksUrl = _configuration["SSO:JwksUrl"] ?? "http://103.143.207.168:32081/api/v1/oauth2/jwks";
            var issuer = _configuration["SSO:IssuerUrl"] ?? "http://103.143.207.168:32081/api/v1";
            var clientId = _configuration["SSO:ClientId"] ?? "qlcv";
            var clockSkewMinutes = _configuration.GetValue<int>("SSO:ClockSkewMinutes", 2);

            var jwks = await GetJwksAsync(jwksUrl, httpClient);
            if (jwks == null || jwks.Keys.Count == 0)
            {
                return (false, null, null, null, "Không thể xác thực chữ ký token: Không tải được danh sách khóa công khai JWKS từ SSO.");
            }

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = jwks.GetSigningKeys(),
                ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(clientId),
                ValidAudience = clientId,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes)
            };

            try
            {
                var principal = handler.ValidateToken(idToken, validationParams, out _);
                var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value
                          ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                var email = principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? principal.FindFirst("email")?.Value
                            ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                var preferredUsername = principal.FindFirst("preferred_username")?.Value
                                        ?? principal.FindFirst("username")?.Value;

                return (true, sub, email, preferredUsername, null);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning(ex, "ID Token từ SSO đã hết hạn.");
                return (false, null, null, null, "ID Token từ hệ thống SSO đã hết hạn.");
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogError(ex, "Chữ ký ID Token từ SSO không hợp lệ.");
                return (false, null, null, null, "Chữ ký số của ID Token không hợp lệ hoặc không khớp với JWKS.");
            }
            catch (SecurityTokenInvalidIssuerException ex)
            {
                _logger.LogError(ex, "Issuer của ID Token không khớp cấu hình.");
                return (false, null, null, null, "Nguồn phát hành (Issuer) của ID Token không hợp lệ.");
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                _logger.LogError(ex, "Audience của ID Token không khớp Client ID.");
                return (false, null, null, null, "Đối tượng nhận (Audience) của ID Token không khớp với Client ID đã đăng ký.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác minh chữ ký và thông tin ID Token.");
                return (false, null, null, null, $"Xác thực ID Token không thành công: {ex.Message}");
            }
        }

        private async Task<JsonWebKeySet?> GetJwksAsync(string jwksUrl, HttpClient httpClient)
        {
            const string cacheKey = "SSO_JWKS_CACHE";
            if (_cache != null && _cache.TryGetValue(cacheKey, out JsonWebKeySet? cachedJwks) && cachedJwks != null)
            {
                return cachedJwks;
            }

            try
            {
                var response = await httpClient.GetStringAsync(jwksUrl);
                var jwks = new JsonWebKeySet(response);
                if (_cache != null && jwks.Keys.Count > 0)
                {
                    _cache.Set(cacheKey, jwks, TimeSpan.FromHours(1));
                }
                return jwks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải JWKS từ SSO endpoint: {JwksUrl}", jwksUrl);
                return null;
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
