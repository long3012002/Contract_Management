using System;

namespace demo1.Entity;

/// <summary>
/// Lưu lịch sử email đã gửi để tránh gửi trùng (chống spam).
/// Mỗi lần handler chuẩn bị gửi email, nó kiểm tra bảng này trước.
/// </summary>
public class EmailNotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>User nhận email.</summary>
    public Guid UserId { get; set; }

    /// <summary>Loại entity: "HopDong", "License", "DotThanhToan", ...</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Id của entity (dạng string để linh hoạt).</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Subject email đã gửi (dùng làm khóa kiểm tra trùng).</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Thời điểm gửi (UTC).</summary>
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
