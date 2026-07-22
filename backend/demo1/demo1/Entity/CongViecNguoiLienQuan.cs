using System;

namespace demo1.Entity;

public class CongViecNguoiLienQuan : BaseEntity
{
    public Guid CongViecGoiThauId { get; set; }
    public virtual CongViecGoiThau? CongViecGoiThau { get; set; }

    public Guid UserId { get; set; }
    public virtual User? User { get; set; }

    /// <summary>
    /// TrangThaiXacNhan: Pending, Confirmed, Commented, Overdue
    /// </summary>
    public string TrangThaiXacNhan { get; set; } = "Pending";

    /// <summary>
    /// Thời hạn phản hồi (Mặc định: CreatedAt + 24 hours)
    /// </summary>
    public DateTime HanXacNhanAt { get; set; } = DateTime.UtcNow.AddHours(24);

    /// <summary>
    /// Thời điểm người dùng thực hiện bấm xác nhận hoặc comment
    /// </summary>
    public DateTime? XacNhanAt { get; set; }

    /// <summary>
    /// Loại xác nhận: DirectConfirm | Comment
    /// </summary>
    public string? LoaiXacNhan { get; set; }

    /// <summary>
    /// Lưu danh sách Job ID do Hangfire trả về (phân cách dấu phẩy) để hủy khi đã xác nhận
    /// </summary>
    public string? ReminderJobIds { get; set; }
}
