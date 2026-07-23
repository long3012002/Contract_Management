using System;

namespace demo1.DTOs;

public class NhaThauGoiThauDto
{
    public Guid Id { get; set; }
    public Guid GoiThauId { get; set; }
    public Guid NhaThauId { get; set; }
    public string? NhaThauName { get; set; }
    public string? NhaThauCode { get; set; }

    public bool IsLienDanh { get; set; }
    public string? TenLienDanh { get; set; }
    public bool IsDaiDienLienDanh { get; set; }
    public decimal TyLeLienDanh { get; set; }
    public decimal GiaTriDamNhan { get; set; }
    public string? VaiTroTrongLienDanh { get; set; }
}
