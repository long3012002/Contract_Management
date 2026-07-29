using System;
using demo1.DTOs;

namespace demo1.DTOs.HangHoa;

public class HangHoaDto : IHasId
{
    public Guid Id { get; set; }
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public string? DanhMucHangHoa { get; set; }
    public string? KyMaHieu { get; set; }
    public string? NhanHieu { get; set; }
    public string? NamSanXuat { get; set; }

    public Guid? IdXuatXu { get; set; }
    public string? TenXuatXu { get; set; }

    public Guid? IdHangSanXuat { get; set; }
    public string? TenHangSanXuat { get; set; }

    public string? CauHinhTinhNangKyThuatCoBan { get; set; }

    public Guid? IdLicense { get; set; }
    public string? TenLicense { get; set; }

    public Guid? IdDonViTinh { get; set; }
    public string? TenDonViTinh { get; set; }

    public int KhoiLuong { get; set; }
    public string? MaHS { get; set; }

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHangHoaDto
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public string? DanhMucHangHoa { get; set; }
    public string? KyMaHieu { get; set; }
    public string? NhanHieu { get; set; }
    public string? NamSanXuat { get; set; }

    public Guid? IdXuatXu { get; set; }
    public Guid? IdHangSanXuat { get; set; }
    public string? CauHinhTinhNangKyThuatCoBan { get; set; }
    public Guid? IdLicense { get; set; }
    public Guid? IdDonViTinh { get; set; }

    public int KhoiLuong { get; set; }
    public string? MaHS { get; set; }

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

public class UpdateHangHoaDto : CreateHangHoaDto
{
}
