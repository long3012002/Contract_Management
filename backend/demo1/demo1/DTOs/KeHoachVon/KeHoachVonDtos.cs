using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class KeHoachVonDto : IHasId
{
    public Guid Id { get; set; }
    public int NamKeHoach { get; set; }
    public int LoaiKeHoach { get; set; }
    public string LoaiKeHoachText => LoaiKeHoach switch
    {
        1 => "6 tháng đầu năm",
        2 => "Cả năm",
        3 => $"Bổ sung (Đợt {DotBoSung ?? 1})",
        _ => "Khác"
    };
    public int? DotBoSung { get; set; }
    public int TrangThai { get; set; }
    public string TrangThaiText => TrangThai switch
    {
        1 => "Nháp (Draft)",
        2 => "Đã trình (Submitted)",
        3 => "Đã duyệt (Approved)",
        4 => "Trả về (Rejected)",
        _ => "Không xác định"
    };

    public string? SoQuyetDinh { get; set; }
    public DateTime? NgayPheDuyet { get; set; }

    public decimal TongMucDeNghi { get; set; }
    public decimal TongMucDuocDuyet { get; set; }
    public string? GhiChu { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<KeHoachVonDuAnItemDto> DanhSachDuAn { get; set; } = new();
    public List<TongNguonVonItemDto> TongTheoNguonVon { get; set; } = new();
}

public class NguonVonChiTietItemDto
{
    public Guid NguonVonId { get; set; }
    public string MaNguonVon { get; set; } = string.Empty;
    public string TenNguonVon { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
}

public class TongNguonVonItemDto
{
    public Guid NguonVonId { get; set; }
    public string MaNguonVon { get; set; } = string.Empty;
    public string TenNguonVon { get; set; } = string.Empty;
    public decimal TongSoTien { get; set; }
}

public class KeHoachVonDuAnItemDto
{
    public Guid DuAnId { get; set; }
    public string MaDuAn { get; set; } = string.Empty;
    public string TenDuAn { get; set; } = string.Empty;
    public decimal SoTienDeNghi { get; set; }
    public decimal SoTienDuocDuyet { get; set; }
    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }
    public string? GhiChu { get; set; }
    public List<NguonVonChiTietItemDto> NguonVonChiTiet { get; set; } = new();
}

public class CreateKeHoachVonDto
{
    public int NamKeHoach { get; set; }
    public int LoaiKeHoach { get; set; }
    public int? DotBoSung { get; set; }
    public string? SoQuyetDinh { get; set; }
    public string? GhiChu { get; set; }

    public List<AddDuAnToKHVDto> DanhSachDuAn { get; set; } = new();
}

public class UpdateKeHoachVonDto
{
    public int NamKeHoach { get; set; }
    public int LoaiKeHoach { get; set; }
    public int? DotBoSung { get; set; }
    public string? SoQuyetDinh { get; set; }
    public string? GhiChu { get; set; }
}

public class ApproveKeHoachVonDto
{
    public string SoQuyetDinh { get; set; } = string.Empty;
    public DateTime NgayPheDuyet { get; set; } = DateTime.Now;
    public List<ApproveKeHoachVonDuAnItemDto> DanhSachDuAnDuyet { get; set; } = new();
}

public class ApproveKeHoachVonDuAnItemDto
{
    public Guid DuAnId { get; set; }
    public decimal SoTienDuocDuyet { get; set; }
    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }
    public string? GhiChu { get; set; }
}

public class AddDuAnToKHVDto
{
    public Guid DuAnId { get; set; }
    public decimal SoTienDeNghi { get; set; }
    public decimal? SoTienDuocDuyet { get; set; }
    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }
    public string? GhiChu { get; set; }
}

public class KeHoachVonFilterDto
{
    public int? Nam { get; set; }
    public int? LoaiKeHoach { get; set; }
    public int? TrangThai { get; set; }
    public string? Search { get; set; }
    public string? DonViTinh { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
