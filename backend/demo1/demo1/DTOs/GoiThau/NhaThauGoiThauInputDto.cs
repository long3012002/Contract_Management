using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class NhaThauGoiThauInputDto
{
    [Required]
    public Guid NhaThauId { get; set; }

    public bool IsLienDanh { get; set; }

    [StringLength(255)]
    public string? TenLienDanh { get; set; }

    public bool IsDaiDienLienDanh { get; set; }

    [Range(0, 100)]
    public decimal TyLeLienDanh { get; set; }

    [Range(0, double.MaxValue)]
    public decimal GiaTriDamNhan { get; set; }

    [StringLength(500)]
    public string? VaiTroTrongLienDanh { get; set; }
}
