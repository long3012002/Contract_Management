using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class OnlyOfficeService : IOnlyOfficeService
    {
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OnlyOfficeService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _storagePath;
        private readonly string _jwtSecret;
        private readonly string _publicBaseUrl;

        public OnlyOfficeService(
            AppDbContext dbContext,
            IConfiguration configuration,
            IWebHostEnvironment env,
            ILogger<OnlyOfficeService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            var uploadSettings = configuration.GetSection("UploadSettings");
            var configPath = uploadSettings["StoragePath"] ?? "uploads";
            _storagePath = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(env.ContentRootPath, configPath);

            var ooSettings = configuration.GetSection("OnlyOfficeSettings");
            _jwtSecret = ooSettings["JwtSecret"] ?? "OnlyOffice_Secret_Key_For_Contract_Management_2026";
            _publicBaseUrl = (ooSettings["PublicBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        }

        public OnlyOfficeConfigDto GenerateConfig(FileAttachment attachment, string mode, Guid userId, string userName)
        {
            var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
            var docType = GetDocumentType(ext);
            var isEditMode = string.Equals(mode, "edit", StringComparison.OrdinalIgnoreCase);

            // Document Key: kết hợp ID và Ticks sửa đổi để vô hiệu hóa cache khi file cập nhật
            var ticks = attachment.UpdatedAt?.Ticks ?? attachment.CreatedAt.Ticks;
            var documentKey = $"FA_{attachment.Id:N}_{ticks}";

            var downloadToken = GenerateDownloadToken(attachment.Id);
            var downloadUrl = $"{_publicBaseUrl}/api/HeThong/files/onlyoffice-download/{attachment.Id}?token={downloadToken}";
            var callbackUrl = $"{_publicBaseUrl}/api/HeThong/files/onlyoffice-callback";

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
                }
            };

            return config;
        }

        public string GenerateDownloadToken(Guid attachmentId)
        {
            // Token dạng: {attachmentId}_{expiryEpoch}_{hmac}
            var expiry = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
            var payload = $"{attachmentId:N}:{expiry}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            
            var rawToken = $"{payload}:{hash}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(rawToken));
        }

        public bool ValidateDownloadToken(Guid attachmentId, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                if (parts.Length != 3) return false;

                var idStr = parts[0];
                if (!long.TryParse(parts[1], out var expiry)) return false;
                var providedHash = parts[2];

                if (!string.Equals(idStr, attachmentId.ToString("N"), StringComparison.OrdinalIgnoreCase))
                    return false;

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiry)
                {
                    _logger.LogWarning("Download token cho FileAttachment {Id} đã hết hạn.", attachmentId);
                    return false;
                }

                var payload = $"{idStr}:{expiry}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
                var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

                return string.Equals(providedHash, expectedHash, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác thực Download Token cho FileAttachment {Id}", attachmentId);
                return false;
            }
        }

        public async Task<bool> HandleCallbackAsync(OnlyOfficeCallbackDto callbackDto)
        {
            if (callbackDto == null || string.IsNullOrWhiteSpace(callbackDto.Key))
            {
                _logger.LogWarning("Callback từ ONLYOFFICE nhận DTO rỗng hoặc không có Document Key.");
                return false;
            }

            _logger.LogInformation("ONLYOFFICE Callback nhận Key={Key}, Status={Status}", callbackDto.Key, callbackDto.Status);

            // Status 2: Document ready for saving
            // Status 6: Document is being edited, force save requested
            if (callbackDto.Status != 2 && callbackDto.Status != 6)
            {
                // Các status khác (1: đang sửa, 4: đóng không sửa...) không cần làm gì
                return true;
            }

            if (string.IsNullOrWhiteSpace(callbackDto.Url))
            {
                _logger.LogWarning("ONLYOFFICE Callback Status={Status} nhưng không có URL tải file mới.", callbackDto.Status);
                return false;
            }

            // Giải mã AttachmentId từ Key: "FA_{attachmentId}_{ticks}"
            var attachmentId = ExtractAttachmentIdFromKey(callbackDto.Key);
            if (attachmentId == Guid.Empty)
            {
                _logger.LogWarning("Không thể trích xuất AttachmentId từ Key: {Key}", callbackDto.Key);
                return false;
            }

            var attachment = await _dbContext.FileAttachments.FirstOrDefaultAsync(f => f.Id == attachmentId && f.IsActive);
            if (attachment == null)
            {
                _logger.LogWarning("Không tìm thấy FileAttachment với Id={AttachmentId} trong CSDL.", attachmentId);
                return false;
            }

            try
            {
                // Tải file mới từ URL do ONLYOFFICE Document Server cung cấp
                var httpClient = _httpClientFactory.CreateClient();
                var newFileBytes = await httpClient.GetByteArrayAsync(callbackDto.Url);

                if (newFileBytes == null || newFileBytes.Length == 0)
                {
                    _logger.LogError("Tải file mới từ ONLYOFFICE thất bại hoặc file 0 bytes. URL={Url}", callbackDto.Url);
                    return false;
                }

                // Ghi đè file cũ trên đĩa
                var fullPath = Path.Combine(_storagePath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
                var targetDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                await File.WriteAllBytesAsync(fullPath, newFileBytes);

                // Cập nhật CSDL
                attachment.FileSize = newFileBytes.Length;
                attachment.UpdatedAt = DateTime.UtcNow;

                _dbContext.FileAttachments.Update(attachment);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Cập nhật thành công FileAttachment {Id} ({FileName}) qua ONLYOFFICE Callback. Kích thước mới: {Size} bytes.",
                    attachment.Id, attachment.FileName, newFileBytes.Length);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý Callback ghi đè file cho FileAttachment {Id}", attachmentId);
                return false;
            }
        }

        private static string GetDocumentType(string extension)
        {
            return extension switch
            {
                ".docx" or ".doc" or ".odt" or ".rtf" or ".txt" => "word",
                ".xlsx" or ".xls" or ".ods" or ".csv" => "cell",
                ".pptx" or ".ppt" or ".odp" => "slide",
                ".pdf" => "word", // ONLYOFFICE hiển thị PDF dưới dạng document type word
                _ => "word"
            };
        }

        private static Guid ExtractAttachmentIdFromKey(string key)
        {
            // Format: "FA_{guid}_{ticks}"
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
