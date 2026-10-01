using System.Threading.Tasks;

namespace demo1.Services.EmailNotifications;

/// <summary>
/// Interface chung cho tất cả các loại email notification.
/// Để thêm một loại email mới, chỉ cần tạo class implement interface này
/// và đăng ký trong ServiceConfiguration.
/// </summary>
public interface IEmailNotificationHandler
{
    /// <summary>
    /// Tên định danh của handler (dùng cho logging).
    /// </summary>
    string HandlerName { get; }

    /// <summary>
    /// Quét và gửi email notification nếu cần.
    /// Mỗi handler tự quyết định đối tượng nào cần gửi và ai nhận.
    /// </summary>
    Task HandleAsync();
}
