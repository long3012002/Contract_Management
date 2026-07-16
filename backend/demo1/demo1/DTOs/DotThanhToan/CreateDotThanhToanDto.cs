using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateDotThanhToanDto
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(255)]
    public string TenDot { get; set; } = string.Empty;

    [Range(0, 100)]
    public decimal TyLeThanhToan { get; set; }

    [Range(0, double.MaxValue)]
    public decimal GiaTriThanhToan { get; set; }

    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
}
