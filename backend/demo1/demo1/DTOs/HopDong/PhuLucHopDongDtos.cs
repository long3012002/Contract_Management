using System;
using System.Collections.Generic;
using demo1.Entity;
using demo1.DTOs.HangHoaDichVu;

namespace demo1.DTOs;

public class PhuLucHopDongDto : IHasId
{
    public Guid Id { get; set; }
    public Guid HopDongId { get; set; }
    public string SoPhuLuc { get; set; } = string.Empty;
    public string TenPhuLuc { get; set; } = string.Empty;
    public LoaiPhuLuc LoaiPhuLuc { get; set; }
    public string LoaiPhuLucText => LoaiPhuLuc switch
    {
        LoaiPhuLuc.DieuChinhGiaTri => "Điều chỉnh giá trị hợp đồng",
        LoaiPhuLuc.GiaHanThoiGian => "Gia hạn thời gian thực hiện",
        LoaiPhuLuc.BoSungHangHoaDichVu => "Bổ sung/thay đổi hàng hóa dịch vụ",
        LoaiPhuLuc.ThayDoiDieuKhoan => "Thay đổi điều khoản/hành chính",
        _ => "Khác"
    };

    public TrangThaiPhuLuc TrangThai { get; set; } = TrangThaiPhuLuc.Active;
    public string TrangThaiText => TrangThai switch
    {
        TrangThaiPhuLuc.Active => "Hiệu lực",
        TrangThaiPhuLuc.Expired => "Hết hiệu lực",
        TrangThaiPhuLuc.Cancelled => "Hủy",
        _ => "Khác"
    };

    public DateTime NgayKy { get; set; }
    public DateTime NgayHieuLuc { get; set; }
    public decimal GiaTriDieuChinh { get; set; }
    public DateTime? ExpiredDateMoi { get; set; }
    public string? ThoiHanThucHienMoi { get; set; }
    public string? NoiDungDieuChinh { get; set; }
    public string? GhiChu { get; set; }

    public List<DotThanhToanDto> DotThanhToans { get; set; } = new();
    public List<HangHoaDichVuDto> HangHoaDichVus { get; set; } = new();
    public List<FileAttachmentDto> FileAttachments { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePhuLucHopDongDto
{
    public Guid HopDongId { get; set; }
    public string SoPhuLuc { get; set; } = string.Empty;
    public string TenPhuLuc { get; set; } = string.Empty;
    public LoaiPhuLuc LoaiPhuLuc { get; set; } = LoaiPhuLuc.DieuChinhGiaTri;
    public TrangThaiPhuLuc TrangThai { get; set; } = TrangThaiPhuLuc.Active;

    public DateTime NgayKy { get; set; } = DateTime.UtcNow;
    public DateTime NgayHieuLuc { get; set; } = DateTime.UtcNow;
    public decimal GiaTriDieuChinh { get; set; } = 0;
    public DateTime? ExpiredDateMoi { get; set; }
    public string? ThoiHanThucHienMoi { get; set; }
    public string? NoiDungDieuChinh { get; set; }
    public string? GhiChu { get; set; }

    public List<CreateDotThanhToanDto>? DotThanhToans { get; set; }
    public List<CreateHangHoaDichVuDto>? HangHoaDichVus { get; set; }
}

public class UpdatePhuLucHopDongDto
{
    public string SoPhuLuc { get; set; } = string.Empty;
    public string TenPhuLuc { get; set; } = string.Empty;
    public LoaiPhuLuc LoaiPhuLuc { get; set; }
    public TrangThaiPhuLuc TrangThai { get; set; }

    public DateTime NgayKy { get; set; }
    public DateTime NgayHieuLuc { get; set; }
    public decimal GiaTriDieuChinh { get; set; }
    public DateTime? ExpiredDateMoi { get; set; }
    public string? ThoiHanThucHienMoi { get; set; }
    public string? NoiDungDieuChinh { get; set; }
    public string? GhiChu { get; set; }
}
