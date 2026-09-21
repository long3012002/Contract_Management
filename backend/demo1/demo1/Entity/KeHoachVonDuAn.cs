using System;

namespace demo1.Entity;

/// <summary>
/// Bảng liên kết M:N giữa KeHoachVon và DuAn, quản lý số tiền đề nghị và được duyệt của từng dự án theo đợt Kế hoạch vốn.
/// </summary>
public class KeHoachVonDuAn
{
    public Guid KeHoachVonId { get; set; }
    public virtual KeHoachVon KeHoachVon { get; set; } = null!;

    public Guid DuAnId { get; set; }
    public virtual DuAn DuAn { get; set; } = null!;

    public decimal SoTienDeNghi { get; set; }
    public decimal SoTienDuocDuyet { get; set; }

    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }

    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
