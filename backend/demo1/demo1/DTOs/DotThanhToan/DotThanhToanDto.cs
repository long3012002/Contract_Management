using System;

namespace demo1.DTOs;

public class DotThanhToanDto
{
    public Guid Id { get; set; }
    public Guid HopDongId { get; set; }
    public string TenDot { get; set; } = string.Empty;
    public decimal TyLeThanhToan { get; set; }
    public decimal GiaTriThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
