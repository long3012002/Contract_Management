using System;
using demo1.Entity;

namespace demo1.DTOs.HangHoaDichVu;

public class HangHoaDichVuDto : IHasId
{
    public Guid Id { get; set; }
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    
    // Type field to distinguish
    public LoaiHangHoaDichVu Loai { get; set; }

    // Hang Hoa specific / common fields
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

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateHangHoaDichVuDto
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public LoaiHangHoaDichVu Loai { get; set; }

    // Hang Hoa fields
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

    // Dich Vu fields
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

public class UpdateHangHoaDichVuDto : CreateHangHoaDichVuDto
{
}
