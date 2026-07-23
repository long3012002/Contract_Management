using System;

namespace demo1.Entity;

public class NhaThauGoiThau : BaseEntity
{
    public Guid GoiThauId { get; set; }
    public virtual GoiThau? GoiThau { get; set; }

    public Guid NhaThauId { get; set; }
    public virtual DoiTac? NhaThau { get; set; }

    public bool IsLienDanh { get; set; }
    public string? TenLienDanh { get; set; }
    public bool? IsDaiDienLienDanh { get; set; }
    public decimal TyLeLienDanh { get; set; }
    public decimal GiaTriDamNhan { get; set; }
    public string? VaiTroTrongLienDanh { get; set; }
}
