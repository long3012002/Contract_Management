using System;

namespace demo1.Entity.DanhMuc;

public class ToNhom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenToNhom { get; set; } = string.Empty;
    public Guid? IdPhongBan { get; set; }
    public PhongBan? PhongBan { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
