using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces
{
    public interface IOnlyOfficeService
    {
        /// <summary>
        /// Tạo cấu hình JSON cho ONLYOFFICE Editor từ tệp đính kèm với kiểm tra phân quyền.
        /// </summary>
        Task<OnlyOfficeConfigDto> GenerateConfigAsync(FileAttachment attachment, string mode, Guid userId, string userName);

        /// <summary>
        /// Tạo Token ngắn hạn (TTL 3 phút) dành riêng cho ONLYOFFICE download.
        /// </summary>
        string GenerateDownloadToken(Guid attachmentId);

        /// <summary>
        /// Xác thực Download Token bảo mật (Kiểm tra mục đích, hết hạn và file binding).
        /// </summary>
        bool ValidateDownloadToken(Guid attachmentId, string token);

        /// <summary>
        /// Xác thực JWT signature từ Header Authorization của ONLYOFFICE Callback.
        /// </summary>
        bool VerifyCallbackJwt(string? authHeader, OnlyOfficeCallbackDto callbackDto);

        /// <summary>
        /// Xử lý Callback từ ONLYOFFICE (Xác thực, Idempotent, Atomic Transaction, File Versioning &amp; Audit Log).
        /// </summary>
        Task<bool> HandleCallbackAsync(OnlyOfficeCallbackDto callbackDto, string? authHeader);

        /// <summary>
        /// Trả về danh sách lịch sử phiên bản của tệp tin đính kèm.
        /// </summary>
        Task<IEnumerable<FileVersionDto>> GetFileVersionsAsync(Guid attachmentId);
    }
}
