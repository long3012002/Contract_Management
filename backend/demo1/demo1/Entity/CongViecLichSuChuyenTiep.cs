using System;

namespace demo1.Entity;

public class CongViecLichSuChuyenTiep : BaseEntity
{
    public Guid CongViecGoiThauId { get; set; }
    public virtual CongViecGoiThau? CongViecGoiThau { get; set; }

    public Guid FromUserId { get; set; }
    public virtual User? FromUser { get; set; }

    public Guid ToUserId { get; set; }
    public virtual User? ToUser { get; set; }

    public string? GhiChu { get; set; }

    /// <summary>
    /// Loại hành động: Initial (Khởi tạo), Forward (Chuyển tiếp), Update (Cập nhật)
    /// </summary>
    public string LoaiHanhDong { get; set; } = "Forward";
}
