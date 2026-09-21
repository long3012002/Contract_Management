using System;

namespace demo1.Entity;

/// <summary>
/// Bảng dữ liệu tham chiếu liên kết gộp dự án (Vừa quản lý mối quan hệ gộp giữa Dự án Nguồn và Dự án Đích, vừa lưu Audit Log thời điểm gộp).
/// </summary>
public class DuAnGopLink : BaseEntity
{
    public Guid SourceDuAnId { get; set; }
    public virtual DuAn SourceDuAn { get; set; } = null!;

    public Guid TargetDuAnId { get; set; }
    public virtual DuAn TargetDuAn { get; set; } = null!;

    public DateTime NgayGop { get; set; } = DateTime.UtcNow;

    public Guid NguoiThucHienId { get; set; }
    public virtual User NguoiThucHien { get; set; } = null!;

    public decimal DuToanLucGop { get; set; }
    public string? GhiChu { get; set; }
}
