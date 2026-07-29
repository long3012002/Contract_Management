using System;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class HangHoa : BaseEntity
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
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

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}
