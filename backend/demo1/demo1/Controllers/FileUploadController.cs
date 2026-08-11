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
using demo1.Services.Interfaces;

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

        /// <summary>
        /// Lấy cấu hình JSON khởi tạo ONLYOFFICE Editor cho một FileAttachment.
        /// </summary>
        /// <param name="id">Mã định danh của FileAttachment (GUID)</param>
        /// <param name="mode">Chế độ: "view" (mặc định) hoặc "edit"</param>
        /// <param name="onlyOfficeService">Service xử lý ONLYOFFICE</param>
        /// <param name="currentUserService">Service người dùng hiện tại</param>
        [HttpGet("{id:guid}/onlyoffice-config")]
        [ProducesResponseType(typeof(OnlyOfficeConfigDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOnlyOfficeConfig(
            Guid id,
            [FromQuery] string mode = "view",
            [FromServices] IOnlyOfficeService onlyOfficeService = null!,
            [FromServices] ICurrentUserService currentUserService = null!)
        {
            try
            {
                var attachment = await _dbContext.FileAttachments.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
                if (attachment == null)
                    return NotFound(new { Message = "Không tìm thấy file đính kèm." });

                var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(ext))
                    return BadRequest(new { Message = "Định dạng file không được hỗ trợ bởi ONLYOFFICE." });

                var userId = currentUserService?.GetUserId() ?? Guid.Empty;
                var userName = currentUserService?.GetUsername() ?? "User";

                var config = await onlyOfficeService.GenerateConfigAsync(attachment, mode, userId, userName);
                return Ok(config);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết hệ thống để debug nội bộ
                // _logger.LogError(ex, "Lỗi khi lấy cấu hình ONLYOFFICE");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi hệ thống khi khởi tạo trình soạn thảo. Vui lòng liên hệ quản trị viên." });
            }
        }

        /// <summary>
        /// Tải xuống tệp tin dành riêng cho máy chủ ONLYOFFICE Document Server (xác thực qua Token bảo mật, hỗ trợ File Streaming).
        /// </summary>
        [HttpGet("onlyoffice-download/{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadForOnlyOffice(
            Guid id,
            [FromQuery] string token,
            [FromServices] IOnlyOfficeService onlyOfficeService = null!)
        {
            try
            {
                if (!onlyOfficeService.ValidateDownloadToken(id, token))
                    return Unauthorized(new { Message = "Download token không hợp lệ hoặc đã hết hạn." });

                var attachment = await _dbContext.FileAttachments.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
                if (attachment == null)
                    return NotFound(new { Message = "Không tìm thấy tệp đính kèm." });

                var fullPath = Path.Combine(_storagePath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(fullPath))
                {
                    // Nếu tệp vật lý chưa có trên đĩa (dữ liệu thử nghiệm/seed), tự khởi tạo tệp mẫu để tránh ngắt quãng 404 khi xem thử
                    var targetDir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    await System.IO.File.WriteAllTextAsync(fullPath, $"Nội dung tài liệu thử nghiệm {attachment.FileName}");
                }

                // Trả về file dưới dạng Streaming (FileStreamResult) tránh nạp toàn bộ file lớn vào RAM
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(fileStream, attachment.ContentType, attachment.FileName);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Lỗi khi tải file ONLYOFFICE");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Đã xảy ra lỗi trong quá trình tải xuống tệp tin." });
            }
        }

        /// <summary>
        /// Callback từ ONLYOFFICE Document Server để cập nhật nội dung tệp tin đính kèm khi sửa đổi xong.
        /// </summary>
        [HttpPost("onlyoffice-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> OnlyOfficeCallback(
            [FromBody] OnlyOfficeCallbackDto dto,
            [FromServices] IOnlyOfficeService onlyOfficeService = null!)
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                var success = await onlyOfficeService.HandleCallbackAsync(dto, authHeader);
                if (success)
                {
                    return Ok(new { error = 0 });
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Lỗi xử lý callback ONLYOFFICE");
            }

            return Ok(new { error = 1 });
        }

        /// <summary>
        /// Lấy danh sách lịch sử các phiên bản của tệp tin đính kèm (FileAttachment).
        /// </summary>
        /// <param name="id">Mã định danh của FileAttachment (GUID)</param>
        /// <param name="onlyOfficeService">Service xử lý ONLYOFFICE</param>
        [HttpGet("{id:guid}/versions")]
        [ProducesResponseType(typeof(IEnumerable<FileVersionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFileVersions(
            Guid id,
            [FromServices] IOnlyOfficeService onlyOfficeService = null!)
        {
            try
            {
                var attachment = await _dbContext.FileAttachments.FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
                if (attachment == null)
                    return NotFound(new { Message = "Không tìm thấy file đính kèm." });

                var versions = await onlyOfficeService.GetFileVersionsAsync(id);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Lỗi lấy danh sách phiên bản file");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Không thể lấy lịch sử phiên bản tệp tin." });
            }
        }

        /// <summary>
        /// Xóa hàng loạt tệp tin đính kèm theo danh sách ID (GUID).
        /// Thực hiện kiểm tra quyền hạn của người dùng đối với thực thể cha chứa file trước khi xóa.
        /// Thực hiện xóa cứng trên CSDL và xóa file vật lý trên đĩa.
        /// </summary>
        /// <param name="ids">Danh sách GUID file cần xóa</param>
        /// <param name="currentUserService">Service người dùng hiện tại để xác thực quyền</param>
        /// <response code="200">Xóa thành công</response>
        /// <response code="400">Danh sách ID rỗng hoặc không hợp lệ</response>
        /// <response code="403">Không có quyền xóa một hoặc nhiều file trong danh sách</response>
        /// <response code="404">Không tìm thấy file nào phù hợp để xóa</response>
        [HttpDelete("delete-multiple")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteMultiple(
            [FromBody] List<Guid> ids,
            [FromServices] ICurrentUserService currentUserService = null!)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(new { Message = "Danh sách ID cần xoá không được để trống." });
            }

            var attachments = await _dbContext.FileAttachments
                .Include(fa => fa.Versions)
                .Where(fa => ids.Contains(fa.Id))
                .ToListAsync();

            if (!attachments.Any())
            {
                return NotFound(new { Message = "Không tìm thấy file nào phù hợp để xóa." });
            }

            var userId = currentUserService?.GetUserId() ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { Message = "Không xác định được danh tính người dùng." });
            }

            // Lấy thông tin user hiện tại và kiểm tra tính hợp lệ
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Tài khoản của bạn đã bị khóa hoặc không hợp lệ." });
            }

            var filesToDelete = new List<FileAttachment>();
            var unauthorizedFileNames = new List<string>();

            foreach (var attachment in attachments)
            {
                // System Admin hoặc người dùng có đủ quyền trên thực thể cha
                if (user.IsSystemAdmin || await HasFileDeletePermissionAsync(userId, attachment))
                {
                    filesToDelete.Add(attachment);
                }
                else
                {
                    unauthorizedFileNames.Add(attachment.FileName);
                }
            }

            // Nếu có ít nhất 1 file không có quyền xóa, chặn toàn bộ yêu cầu để bảo đảm tính nhất quán
            if (unauthorizedFileNames.Any())
            {
                return StatusCode(StatusCodes.Status403Forbidden, new 
                { 
                    Message = "Bạn không có quyền xóa một số file trong danh sách yêu cầu.", 
                    UnauthorizedFiles = unauthorizedFileNames 
                });
            }

            foreach (var attachment in filesToDelete)
            {
                // 1. Xóa file vật lý trên server (cả file gốc và các phiên bản cũ)
                try
                {
                    var mainFullPath = Path.Combine(_storagePath, attachment.FilePath.Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(mainFullPath))
                    {
                        System.IO.File.Delete(mainFullPath);
                    }

                    if (attachment.Versions != null)
                    {
                        foreach (var version in attachment.Versions)
                        {
                            var versionFullPath = Path.Combine(_storagePath, version.FilePath.Replace('/', Path.DirectorySeparatorChar));
                            if (System.IO.File.Exists(versionFullPath))
                            {
                                System.IO.File.Delete(versionFullPath);
                            }
                        }
                    }

                    // Xóa thư mục cha nếu rỗng
                    var parentDir = Path.GetDirectoryName(mainFullPath);
                    if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir) && !Directory.EnumerateFileSystemEntries(parentDir).Any())
                    {
                        Directory.Delete(parentDir);
                    }
                }
                catch (Exception)
                {
                    // Chỉ bỏ qua lỗi đĩa, không chặn luồng cập nhật DB
                }

                // 2. Xóa các bản ghi FileVersion liên quan
                if (attachment.Versions != null && attachment.Versions.Any())
                {
                    _dbContext.FileVersions.RemoveRange(attachment.Versions);
                }
            }

            // 3. Xóa các bản ghi FileAttachment liên quan
            _dbContext.FileAttachments.RemoveRange(filesToDelete);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Đã xóa cứng thành công {filesToDelete.Count} file.",
                DeletedIds = filesToDelete.Select(a => a.Id).ToList()
            });
        }

        /// <summary>
        /// Helper kiểm tra xem người dùng có quyền xóa file dựa trên thực thể cha chứa file đó hay không.
        /// </summary>
        private async Task<bool> HasFileDeletePermissionAsync(Guid userId, FileAttachment attachment)
        {
            var entityId = attachment.EntityId;
            var featureCode = attachment.EntityType;

            // 1. Tìm Project ID tương ứng chứa thực thể cha
            Guid? duAnId = null;
            if (featureCode == "DU_AN")
            {
                duAnId = entityId;
            }
            else if (featureCode == "GOI_THAU")
            {
                var gt = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entityId);
                duAnId = gt?.DuAnId;
                if (duAnId == null)
                {
                    var cv = await _dbContext.CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entityId);
                    if (cv != null)
                    {
                        var pgt = await _dbContext.GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cv.GoiThauId);
                        duAnId = pgt?.DuAnId;
                    }
                }
            }
            else if (featureCode == "QUAN_LY_HOP_DONG")
            {
                var hd = await _dbContext.HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == entityId);
                duAnId = hd?.DuAnId;
            }

            // 2. Nếu là Chủ dự án (Project Owner) -> Có toàn quyền edit/delete đối với tất cả tài nguyên con thuộc dự án
            if (duAnId.HasValue)
            {
                var isProjectOwner = await _dbContext.DuAns.AnyAsync(da => da.Id == duAnId.Value && da.CreatedByUserId == userId);
                if (isProjectOwner) return true;
            }

            // 3. Nếu không phải chủ dự án, kiểm tra quyền chi tiết trong bảng UserPermissions (yêu cầu quyền DELETE trên thực thể cha)
            var hasPermission = await _dbContext.UserPermissions
                .AsNoTracking()
                .Include(up => up.Permission)
                .AnyAsync(up =>
                    up.UserId == userId &&
                    (
                        ((up.FeatureCode == featureCode || up.FeatureCode == string.Empty) && up.EntityId == entityId.ToString()) ||
                        (duAnId.HasValue && up.FeatureCode == "DU_AN" && up.DuAnId == duAnId.Value)
                    ) &&
                    up.Permission != null && up.Permission.Code == "DELETE");

            return hasPermission;
        }
    }
}
