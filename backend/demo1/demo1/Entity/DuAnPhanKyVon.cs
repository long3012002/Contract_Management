using System;

namespace demo1.Entity;

public class DuAnPhanKyVon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DuAnId { get; set; }
    public virtual DuAn DuAn { get; set; } = null!;

    public int Nam { get; set; }
    public decimal SoTienPhanKy { get; set; }
    public decimal? TyLePercent { get; set; }
    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
