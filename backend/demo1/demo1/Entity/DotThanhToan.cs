using System;

namespace demo1.Entity;

public class DotThanhToan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HopDongId { get; set; }
    public virtual HopDong HopDong { get; set; } = null!;

    public string TenDot { get; set; } = string.Empty;
    public decimal TyLeThanhToan { get; set; }
    public decimal GiaTriThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
    public bool IsPaid { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
