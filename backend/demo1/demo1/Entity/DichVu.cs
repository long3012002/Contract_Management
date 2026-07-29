using System;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class DichVu : BaseEntity
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public string? TenDichVu { get; set; }
    public string? MoTaDichVu { get; set; }
    public int KhoiLuong { get; set; }

    public Guid? IdDonViTinh { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }

    public string? DiaDiemThucHienDichVu { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public string? ThoiHan { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? NgayHoanThanhDichVu { get; set; }

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}
