using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using demo1.Data;
using demo1.Entity;
using demo1.DTOs;

namespace demo1.Controllers
{
    /// <summary>
    /// API Quản lý Tải lên và Tải xuống tài liệu đính kèm (FileAttachments).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/HeThong/files")]
    public class FileUploadController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly string _storagePath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedExtensions;

        public FileUploadController(
            AppDbContext dbContext,
            IConfiguration configuration, 
            IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            
            var uploadSettings = configuration.GetSection("UploadSettings");
            var configPath = uploadSettings["StoragePath"] ?? "uploads";

            // Nếu là đường dẫn tương đối, kết hợp với ContentRootPath của dự án
            _storagePath = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(env.ContentRootPath, configPath);

            _maxFileSize = uploadSettings.GetValue<long>("MaxFileSizeBytes", 52428800); // Mặc định 50MB
            _allowedExtensions = uploadSettings.GetSection("AllowedExtensions").Get<string[]>() 
                                 ?? new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".png", ".jpg", ".jpeg" };
        }

        /// <summary>
        /// Tải lên một tệp tin đính kèm và lưu thông tin vào cơ sở dữ liệu.
        /// Thư mục lưu trữ dạng: {StoragePath}/{FileAttachmentId}/{FeatureCode}/{EntityId}/{FileName}
        /// </summary>
        /// <param name="file">Tệp tin cần tải lên</param>
        /// <param name="featureCode">Mã tính năng hệ thống (ví dụ: QUAN_LY_HOP_DONG)</param>
        /// <param name="entityId">Mã thực thể liên quan (ví dụ: GUID của Hợp đồng)</param>
        /// <response code="200">Tải lên thành công</response>
        /// <response code="400">Tệp tin hoặc tham số không hợp lệ</response>
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile(
            IFormFile file, 
            [FromForm] string featureCode, 
            [FromForm] Guid entityId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "File không hợp lệ hoặc rỗng." });

            if (file.Length > _maxFileSize)
                return BadRequest(new { Message = $"File vượt quá giới hạn cho phép ({_maxFileSize / 1024 / 1024} MB)." });

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!_allowedExtensions.Contains(fileExtension))
                return BadRequest(new { Message = "Định dạng file không được hỗ trợ." });

            if (string.IsNullOrWhiteSpace(featureCode))
                return BadRequest(new { Message = "Mã tính năng (featureCode) không được để trống." });

            if (entityId == Guid.Empty)
                return BadRequest(new { Message = "Mã thực thể (entityId) không hợp lệ." });

            try
            {
                // 1. Tạo unique ID cho FileAttachment
                var fileAttachmentId = Guid.NewGuid();

                // 2. Xây dựng đường dẫn vật lý và đường dẫn tương đối
                var targetDir = Path.Combine(_storagePath, fileAttachmentId.ToString(), featureCode.Trim(), entityId.ToString());
                
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

                var physicalPath = Path.Combine(targetDir, file.FileName);
                var relativePath = Path.Combine(fileAttachmentId.ToString(), featureCode.Trim(), entityId.ToString(), file.FileName)
                                       .Replace('\\', '/');

                // 3. Lưu file vật lý xuống đĩa
                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 4. Tạo bản ghi trong cơ sở dữ liệu
                var fileAttachment = new FileAttachment
                {
                    Id = fileAttachmentId,
                    FileName = file.FileName,
                    FilePath = relativePath,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    FileSize = file.Length,
                    EntityType = featureCode.Trim(),
                    EntityId = entityId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.FileAttachments.Add(fileAttachment);
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    FileAttachmentId = fileAttachment.Id,
                    FileName = fileAttachment.FileName,
                    RelativePath = fileAttachment.FilePath,
                    ContentType = fileAttachment.ContentType,
                    FileSize = fileAttachment.FileSize,
                    CreatedAt = fileAttachment.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new 
                { 
                    Message = "Đã xảy ra lỗi trong quá trình upload file.", 
                    Detail = ex.Message 
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tài liệu đính kèm của một bản ghi chức năng cụ thể.
        /// </summary>
        /// <param name="featureCode">Mã tính năng hệ thống (ví dụ: QUAN_LY_HOP_DONG)</param>
        /// <param name="entityId">Mã định danh thực thể chức năng (GUID)</param>
        /// <response code="200">Trả về danh sách tài liệu đính kèm</response>
        [HttpGet("by-entity")]
        [ProducesResponseType(typeof(IEnumerable<FileAttachmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FileAttachmentDto>>> GetAttachmentsByEntity(
            [FromQuery] string featureCode, 
            [FromQuery] Guid entityId)
        {
            if (string.IsNullOrWhiteSpace(featureCode))
                return BadRequest(new { Message = "Mã tính năng (featureCode) không được để trống." });

            if (entityId == Guid.Empty)
                return BadRequest(new { Message = "Mã thực thể (entityId) không hợp lệ." });

            var attachments = await _dbContext.FileAttachments
                .Where(fa => fa.EntityType == featureCode.Trim() && fa.EntityId == entityId && fa.IsActive)
                .OrderByDescending(fa => fa.CreatedAt)
                .Select(fa => new FileAttachmentDto
                {
                    Id = fa.Id,
                    FileName = fa.FileName,
                    FilePath = fa.FilePath,
                    ContentType = fa.ContentType,
                    FileSize = fa.FileSize,
                    CreatedAt = fa.CreatedAt
                })
                .ToListAsync();

            return Ok(attachments);
        }

        /// <summary>
        /// Tải xuống tệp tin đính kèm bằng mã định danh duy nhất (GUID).
        /// Khuyên dùng cho môi trường bảo mật cao.
        /// </summary>
        /// <param name="id">Mã định danh của FileAttachment (GUID)</param>
        /// <response code="200">Trả về file stream</response>
        /// <response code="404">Không tìm thấy file</response>
        [HttpGet("download/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFileById(Guid id)
        {
            var attachment = await _dbContext.FileAttachments.FindAsync(id);
            if (attachment == null || !attachment.IsActive)
                return NotFound(new { Message = "Không tìm thấy file đính kèm." });

            var fullPath = Path.Combine(_storagePath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { Message = "Tệp tin không tồn tại trên ổ đĩa server." });

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, attachment.ContentType, attachment.FileName);
        }

        /// <summary>
        /// Tải xuống tệp tin đính kèm bằng đường dẫn tương đối (Relative Path).
        /// </summary>
        /// <param name="relativePath">Đường dẫn tương đối của file</param>
        /// <response code="200">Trả về file stream</response>
        /// <response code="400">Tham số hoặc đường dẫn không hợp lệ (Directory Traversal)</response>
        /// <response code="404">Không tìm thấy file</response>
        [HttpGet("download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadFileByPath([FromQuery] string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return BadRequest(new { Message = "Đường dẫn file không được để trống." });

            // Ngăn chặn Directory Traversal
            if (relativePath.Contains("..") || relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
                return BadRequest(new { Message = "Đường dẫn yêu cầu không hợp lệ." });

            var cleanPath = relativePath.Replace('\\', '/');
            var fullPath = Path.Combine(_storagePath, cleanPath.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { Message = "Tệp tin không tồn tại trên hệ thống." });

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var originalFileName = Path.GetFileName(fullPath);
            var contentType = "application/octet-stream";

            // Thử tra cứu thông tin tên file và loại file gốc từ cơ sở dữ liệu
            var attachment = await _dbContext.FileAttachments
                .FirstOrDefaultAsync(fa => fa.FilePath == cleanPath && fa.IsActive);
            if (attachment != null)
            {
                originalFileName = attachment.FileName;
                contentType = attachment.ContentType;
            }
            else
            {
                contentType = GetContentType(fullPath);
            }

            return File(bytes, contentType, originalFileName);
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream",
            };
        }
    }
}
