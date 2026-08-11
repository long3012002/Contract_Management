using System;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces
{
    public interface IOnlyOfficeService
    {
        /// <summary>
        /// Tạo cấu hình JSON cho ONLYOFFICE Editor từ một tệp đính kèm (FileAttachment).
        /// </summary>
        OnlyOfficeConfigDto GenerateConfig(FileAttachment attachment, string mode, Guid userId, string userName);

        /// <summary>
        /// Tạo Token ngắn hạn cho phép ONLYOFFICE Server tải tệp đính kèm.
        /// </summary>
        string GenerateDownloadToken(Guid attachmentId);

        /// <summary>
        /// Xác thực Token ngắn hạn khi ONLYOFFICE Server yêu cầu tải tệp.
        /// </summary>
        bool ValidateDownloadToken(Guid attachmentId, string token);

        /// <summary>
        /// Xử lý Callback khi ONLYOFFICE Server gửi thông báo trạng thái cập nhật tệp.
        /// </summary>
        Task<bool> HandleCallbackAsync(OnlyOfficeCallbackDto callbackDto);
    }
}
