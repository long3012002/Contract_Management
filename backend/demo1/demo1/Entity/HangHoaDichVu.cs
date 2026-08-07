using System;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class HangHoaDichVu : BaseEntity
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    
    // Field to distinguish the type: HangHoa, License, DichVu
    public LoaiHangHoaDichVu Loai { get; set; }

    // Hang Hoa specific / common fields
    public string? DanhMucHangHoa { get; set; }
    public string? KyMaHieu { get; set; }
    public string? NhanHieu { get; set; }
    public string? NamSanXuat { get; set; }

    public Guid? IdXuatXu { get; set; }
    public virtual XuatXu? XuatXu { get; set; }

    public Guid? IdHangSanXuat { get; set; }
    public virtual HangSanXuat? HangSanXuat { get; set; }

    public string? CauHinhTinhNangKyThuatCoBan { get; set; }

    public Guid? IdLicense { get; set; }
    public virtual License? License { get; set; }

    public Guid? IdDonViTinh { get; set; }
    public virtual DonViTinh? DonViTinh { get; set; }

    public int KhoiLuong { get; set; }
    public string? MaHS { get; set; }

    // Dich Vu specific fields
    public string? TenDichVu { get; set; }
    public string? MoTaDichVu { get; set; }
    public string? DiaDiemThucHienDichVu { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public string? ThoiHan { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? NgayHoanThanhDichVu { get; set; }

    // Common fields
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}
