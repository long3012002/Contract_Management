using System;

namespace demo1.Entity;

public class NhaThauGoiThau : BaseEntity
{
    public Guid HopDongId { get; set; }
    public virtual HopDong? HopDong { get; set; }

    public Guid NhaThauId { get; set; }
    public virtual DoiTac? NhaThau { get; set; }

    public bool IsLienDanh { get; set; }

}
