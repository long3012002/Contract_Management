using System;

namespace demo1.Entity;

public class NhaThauGoiThau
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HopDongId { get; set; }
    public virtual HopDong? HopDong { get; set; }

    public Guid NhaThauId { get; set; }
    public virtual DoiTac? NhaThau { get; set; }

    public bool IsLienDanh { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
