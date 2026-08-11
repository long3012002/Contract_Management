using System;
using System.Collections.Generic;
using System.IO;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class OnlyOfficeService : IOnlyOfficeService
    {
        private readonly AppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OnlyOfficeService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _storagePath;
        private readonly string _jwtSecret;
        private readonly string _publicBaseUrl;
        private readonly string _onlyOfficeServerUrl;

        public OnlyOfficeService(
            AppDbContext dbContext,
            IPermissionService permissionService,
            IConfiguration configuration,
            IWebHostEnvironment env,
            ILogger<OnlyOfficeService> logger,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;

            var uploadSettings = configuration.GetSection("UploadSettings");
            var configPath = uploadSettings["StoragePath"] ?? "uploads";
            _storagePath = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(env.ContentRootPath, configPath);

            var ooSettings = configuration.GetSection("OnlyOfficeSettings");
            _jwtSecret = ooSettings["JwtSecret"] ?? "OnlyOffice_Secret_Key_For_Contract_Management_2026";
            _publicBaseUrl = (ooSettings["PublicBaseUrl"] ?? "http://10.225.11.201:64950").TrimEnd('/');
            _onlyOfficeServerUrl = (ooSettings["ServerUrl"] ?? "http://localhost:8080").TrimEnd('/');
        }

        private string GetPublicBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var scheme = request.Scheme;
                var hostStr = request.Host.Value;

                if (hostStr.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) || hostStr.StartsWith("127.0.0.1"))
                {
                    var portSuffix = request.Host.Port.HasValue ? $":{request.Host.Port.Value}" : "";
                    return $"{scheme}://10.225.11.201{portSuffix}";
                }

                return $"{scheme}://{hostStr}";
            }

            return _publicBaseUrl;
        }

        public async Task<OnlyOfficeConfigDto> GenerateConfigAsync(FileAttachment attachment, string mode, Guid userId, string userName)
        {
            var isEditMode = string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase);

            if (isEditMode)
            {
                var hasEditPermission = await _permissionService.HasPermissionAsync(
                    userId, attachment.EntityType, attachment.EntityType, attachment.EntityId.ToString(), "EDIT");

                if (!hasEditPermission)
                {
                    _logger.LogWarning("User {UserId} ({UserName}) bị từ chối quyền EDIT đối với FileAttachment {FileId} (Entity: {EntityType}/{EntityId})",
                        userId, userName, attachment.Id, attachment.EntityType, attachment.EntityId);
                    throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa tệp tin đính kèm này.");
                }
            }

            var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            var docType = GetDocumentType(ext);

            var ticks = attachment.UpdatedAt?.Ticks ?? attachment.CreatedAt.Ticks;
            var documentKey = $"FA_{attachment.Id:N}_v{attachment.CurrentVersion}_{ticks}";

            var downloadToken = GenerateDownloadToken(attachment.Id);
            var activeBaseUrl = GetPublicBaseUrl();
            var downloadUrl = $"{activeBaseUrl}/api/HeThong/files/onlyoffice-download/{attachment.Id}?token={downloadToken}";
            var callbackUrl = $"{activeBaseUrl}/api/HeThong/files/onlyoffice-callback";

            var config = new OnlyOfficeConfigDto
            {
                DocumentType = docType,
                Document = new DocumentInfo
                {
                    FileType = ext.TrimStart('.'),
                    Key = documentKey,
                    Title = attachment.FileName,
                    Url = downloadUrl,
                    Permissions = new DocumentPermissions
                    {
                        Comment = true,
                        Copy = true,
                        Download = true,
                        Edit = isEditMode,
                        Print = true,
                        Review = isEditMode
                    }
                },
                EditorConfig = new EditorConfigInfo
                {
                    Mode = isEditMode ? "edit" : "view",
                    Lang = "vi",
                    CallbackUrl = callbackUrl,
                    User = new UserInfo
                    {
                        Id = userId.ToString(),
                        Name = string.IsNullOrWhiteSpace(userName) ? "Người dùng" : userName
                    },
                    Customization = new CustomizationInfo
                    {
                        Autosave = true,
                        Forcesave = true,
                        Chat = false,
                        Comments = true,
                        CompactHeader = false
                    }
                },
                ServerUrl = _onlyOfficeServerUrl
            };

            // Tạo mã hóa JWT cho đối tượng Config gửi sang ONLYOFFICE nếu cấu hình JwtSecret
            config.Token = SignJwtToken(config);

            return config;
        }

        public string GenerateDownloadToken(Guid attachmentId)
        {
            // TTL = 15 phút, Purpose = "onlyoffice-download", Binding = attachmentId
            var expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
            var payload = $"id={attachmentId:N}&purpose=onlyoffice-download&exp={expiry}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
            var hash = Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

            var raw = $"{payload}&sig={hash}";
            return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(raw));
        }

        public bool ValidateDownloadToken(Guid attachmentId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var decodedBytes = Base64UrlEncoder.DecodeBytes(token);
                var decoded = Encoding.UTF8.GetString(decodedBytes);
                var queryParams = decoded.Split('&').Select(p => p.Split('=')).ToDictionary(p => p[0], p => p.Length > 1 ? p[1] : "");

                if (!queryParams.TryGetValue("id", out var idStr) ||
                    !queryParams.TryGetValue("purpose", out var purpose) ||
                    !queryParams.TryGetValue("exp", out var expStr) ||
                    !queryParams.TryGetValue("sig", out var providedSig))
                {
                    return false;
                }

                // 1. Kiểm tra purpose
                if (purpose != "onlyoffice-download") return false;

                // 2. Kiểm tra file binding
                if (!string.Equals(idStr, attachmentId.ToString("N"), StringComparison.OrdinalIgnoreCase))
                    return false;

                // 3. Kiểm tra hết hạn (TTL 15 phút)
                if (!long.TryParse(expStr, out var expiry) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                {
                    _logger.LogWarning("Download token cho FileAttachment {Id} đã hết hạn.", attachmentId);
                    return false;
                }

                // 4. Kiểm tra HMAC signature
                var payload = $"id={idStr}&purpose={purpose}&exp={expStr}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
                var expectedSig = Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

                return string.Equals(providedSig, expectedSig, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác thực Download Token cho FileAttachment {Id}", attachmentId);
                return false;
            }
        }

        public bool VerifyCallbackJwt(string? authHeader, OnlyOfficeCallbackDto callbackDto)
        {
            if (string.IsNullOrWhiteSpace(authHeader) && string.IsNullOrWhiteSpace(callbackDto.Token))
            {
                // Nếu chưa cấu hình bật bắt buộc JWT xác thực trên ONLYOFFICE Server thì tạm cho qua và log
                _logger.LogInformation("Callback không chứa JWT Auth Token Header.");
                return true;
            }

            var token = !string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : callbackDto.Token;

            if (string.IsNullOrWhiteSpace(token)) return true;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromMinutes(2)
                }, out _);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác thực JWT Signature cho ONLYOFFICE Callback.");
                return false;
            }
        }

        public async Task<bool> HandleCallbackAsync(OnlyOfficeCallbackDto callbackDto, string? authHeader)
        {
            if (callbackDto == null || string.IsNullOrWhiteSpace(callbackDto.Key))
            {
                _logger.LogWarning("Callback từ ONLYOFFICE nhận DTO rỗng hoặc không có Document Key.");
                return false;
            }

            // 1. Xác thực JWT Signature từ ONLYOFFICE Server
            if (!VerifyCallbackJwt(authHeader, callbackDto))
            {
                _logger.LogWarning("Xác thực JWT Callback từ ONLYOFFICE thất bại cho Key={Key}", callbackDto.Key);
                return false;
            }

            _logger.LogInformation("ONLYOFFICE Callback nhận Key={Key}, Status={Status}", callbackDto.Key, callbackDto.Status);

            // Xử lý status:
            // Status 1: Người dùng đang chỉnh sửa (document editing active) -> OK
            // Status 4: Người dùng đóng tab không thay đổi -> OK
            // Status 7: Lỗi corrupt khi force save -> Log warning
            if (callbackDto.Status == 1 || callbackDto.Status == 4)
            {
                return true;
            }

            if (callbackDto.Status == 7)
            {
                _logger.LogWarning("ONLYOFFICE Callback nhận Status=7 (Corrupt document) cho Key={Key}", callbackDto.Key);
                return true;
            }

            // Chỉ xử lý tải & lưu file mới khi Status = 2 (ready for save) hoặc Status = 6 (force save)
            if (callbackDto.Status != 2 && callbackDto.Status != 6)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(callbackDto.Url))
            {
                _logger.LogWarning("ONLYOFFICE Callback Status={Status} nhưng không có URL download.", callbackDto.Status);
                return false;
            }

            // Anti-SSRF Check: Validate URL download
            if (!ValidateCallbackUrl(callbackDto.Url))
            {
                _logger.LogError("Cảnh báo Anti-SSRF: URL download callback không hợp lệ hoặc bị từ chối: {Url}", callbackDto.Url);
                return false;
            }

            // Extract FileAttachmentId từ key "FA_{guid}_v{ver}_{ticks}"
            var attachmentId = ExtractAttachmentIdFromKey(callbackDto.Key);
            if (attachmentId == Guid.Empty)
            {
                _logger.LogWarning("Không thể trích xuất AttachmentId hợp lệ từ Key: {Key}", callbackDto.Key);
                return false;
            }

            var attachment = await _dbContext.FileAttachments
                .Include(f => f.Versions)
                .FirstOrDefaultAsync(f => f.Id == attachmentId && f.IsActive);

            if (attachment == null)
            {
                _logger.LogWarning("Không tìm thấy FileAttachment với Id={AttachmentId} trong CSDL.", attachmentId);
                return false;
            }

            // Idempotency Check: Nếu phiên bản kế tiếp đã tồn tại cho key này thì bỏ qua
            var nextVersionNumber = attachment.CurrentVersion + 1;
            var isAlreadyProcessed = attachment.Versions.Any(v => v.VersionNumber == nextVersionNumber);
            if (isAlreadyProcessed)
            {
                _logger.LogInformation("Callback Idempotent: Phiên bản v{Version} của FileAttachment {Id} đã được xử lý trước đó.",
                    nextVersionNumber, attachmentId);
                return true;
            }

            // BẮT ĐẦU ATOMIC TRANSACTION
            using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();
            string? newPhysicalFilePath = null;

            try
            {
                // Tải file mới từ ONLYOFFICE Document Server
                var httpClient = _httpClientFactory.CreateClient();
                var newFileBytes = await httpClient.GetByteArrayAsync(callbackDto.Url);

                if (newFileBytes == null || newFileBytes.Length == 0)
                {
                    _logger.LogError("Tải file mới từ ONLYOFFICE thất bại hoặc file rỗng (0 bytes). URL={Url}", callbackDto.Url);
                    await dbTransaction.RollbackAsync();
                    return false;
                }

                // Xây dựng đường dẫn lưu trữ phiên bản mới: uploads/{attachmentId}/v{nextVersionNumber}/{fileName}
                var relativeVersionDir = Path.Combine(attachment.Id.ToString(), $"v{nextVersionNumber}").Replace('\\', '/');
                var physicalVersionDir = Path.Combine(_storagePath, relativeVersionDir.Replace('/', Path.DirectorySeparatorChar));

                if (!Directory.Exists(physicalVersionDir))
                {
                    Directory.CreateDirectory(physicalVersionDir);
                }

                newPhysicalFilePath = Path.Combine(physicalVersionDir, attachment.FileName);
                await File.WriteAllBytesAsync(newPhysicalFilePath, newFileBytes);

                var relativeFilePath = Path.Combine(relativeVersionDir, attachment.FileName).Replace('\\', '/');
                var editorUserName = callbackDto.Users?.FirstOrDefault() ?? "ONLYOFFICE User";

                // 1. Tạo bản ghi FileVersion
                var fileVersion = new FileVersion
                {
                    Id = Guid.NewGuid(),
                    FileAttachmentId = attachment.Id,
                    VersionNumber = nextVersionNumber,
                    FileName = attachment.FileName,
                    FilePath = relativeFilePath,
                    FileSize = newFileBytes.Length,
                    ContentType = attachment.ContentType,
                    CreatedByUserId = null,
                    CreatedByUserName = editorUserName,
                    CreatedAt = DateTime.UtcNow,
                    ChangeDescription = $"Cập nhật phiên bản v{nextVersionNumber} từ ONLYOFFICE Editor."
                };
                _dbContext.FileVersions.Add(fileVersion);

                // 2. Cập nhật FileAttachment trỏ tới CurrentVersion mới
                attachment.CurrentVersion = nextVersionNumber;
                attachment.FilePath = relativeFilePath;
                attachment.FileSize = newFileBytes.Length;
                attachment.UpdatedAt = DateTime.UtcNow;
                _dbContext.FileAttachments.Update(attachment);

                // 3. Tạo AuditLog
                var auditLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.Empty.ToString(),
                    Username = editorUserName,
                    Action = "UPDATE_DOCUMENT_VERSION",
                    TableName = "FileAttachment",
                    EntityId = attachment.Id.ToString(),
                    OldValues = $"CurrentVersion: {nextVersionNumber - 1}",
                    NewValues = $"CurrentVersion: {nextVersionNumber}, FileSize: {newFileBytes.Length} bytes",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = "ONLYOFFICE Callback"
                };
                _dbContext.AuditLogs.Add(auditLog);

                // Save DB & Commit Transaction
                await _dbContext.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("Tạo phiên bản mới v{Version} thành công cho FileAttachment {Id} ({FileName}). Kích thước: {Size} bytes.",
                    nextVersionNumber, attachment.Id, attachment.FileName, newFileBytes.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong quá trình lưu phiên bản mới cho FileAttachment {Id}. Đang rollback...", attachmentId);
                await dbTransaction.RollbackAsync();

                // Clean up orphan file nếu đã ghi đĩa
                if (!string.IsNullOrEmpty(newPhysicalFilePath) && File.Exists(newPhysicalFilePath))
                {
                    try { File.Delete(newPhysicalFilePath); } catch { }
                }

                return false;
            }
        }

        public async Task<IEnumerable<FileVersionDto>> GetFileVersionsAsync(Guid attachmentId)
        {
            var versions = await _dbContext.FileVersions
                .AsNoTracking()
                .Where(v => v.FileAttachmentId == attachmentId && v.IsActive)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new FileVersionDto
                {
                    Id = v.Id,
                    FileAttachmentId = v.FileAttachmentId,
                    VersionNumber = v.VersionNumber,
                    FileName = v.FileName,
                    FilePath = v.FilePath,
                    FileSize = v.FileSize,
                    ContentType = v.ContentType,
                    CreatedByUserId = v.CreatedByUserId,
                    CreatedByUserName = v.CreatedByUserName,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            return versions;
        }

        private bool ValidateCallbackUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

            // Kiểm tra host của callback URL không được trỏ vào private IP bất hợp pháp ngoại trừ server ONLYOFFICE đã cấu hình
            return true;
        }

        private string SignJwtToken(OnlyOfficeConfigDto config)
        {
            try
            {
                var payloadObj = new
                {
                    documentType = config.DocumentType,
                    document = config.Document,
                    editorConfig = config.EditorConfig
                };

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var headerJson = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(payloadObj, options);

                var headerBase64 = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(headerJson));
                var payloadBase64 = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payloadJson));

                var unsignedToken = $"{headerBase64}.{payloadBase64}";

                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
                var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
                var signatureBase64 = Base64UrlEncoder.Encode(signatureBytes);

                return $"{unsignedToken}.{signatureBase64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sinh JWT Signature cho ONLYOFFICE Config.");
                return string.Empty;
            }
        }

        private static string GetDocumentType(string extension)
        {
            return extension switch
            {
                ".docx" or ".doc" or ".odt" or ".rtf" or ".txt" => "word",
                ".xlsx" or ".xls" or ".ods" or ".csv" => "cell",
                ".pptx" or ".ppt" or ".odp" => "slide",
                ".pdf" => "word",
                _ => "word"
            };
        }

        private static Guid ExtractAttachmentIdFromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return Guid.Empty;

            var parts = key.Split('_');
            if (parts.Length >= 2 && Guid.TryParse(parts[1], out var guid))
            {
                return guid;
            }

            return Guid.Empty;
        }
    }
}
