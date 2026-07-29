using System;
using demo1.DTOs;

namespace demo1.DTOs.DichVu;

public class DichVuDto : IHasId
{
    public Guid Id { get; set; }
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public string? TenDichVu { get; set; }
    public string? MoTaDichVu { get; set; }
    public int KhoiLuong { get; set; }

    public Guid? IdDonViTinh { get; set; }
    public string? TenDonViTinh { get; set; }

    public string? DiaDiemThucHienDichVu { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public string? ThoiHan { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? NgayHoanThanhDichVu { get; set; }

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDichVuDto
{
    public Guid IdParent { get; set; }

    public string? Stt { get; set; }
    public string? TenDichVu { get; set; }
    public string? MoTaDichVu { get; set; }
    public int KhoiLuong { get; set; }

    public Guid? IdDonViTinh { get; set; }

    public string? DiaDiemThucHienDichVu { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public string? ThoiHan { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string? NgayHoanThanhDichVu { get; set; }

    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
}

public class UpdateDichVuDto : CreateDichVuDto
{
}
