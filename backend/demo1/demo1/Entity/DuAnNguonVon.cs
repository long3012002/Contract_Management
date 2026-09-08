using System;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class DuAnNguonVon
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DuAnId { get; set; }
    public virtual DuAn DuAn { get; set; } = null!;

    public Guid NguonVonId { get; set; }
    public virtual NguonVon? NguonVon { get; set; }

    public decimal SoTien { get; set; }
    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
