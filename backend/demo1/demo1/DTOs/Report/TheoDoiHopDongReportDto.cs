using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class TheoDoiHopDongDotThanhToanDto
{
    public Guid Id { get; set; }
    public string TenDot { get; set; } = string.Empty;
    public decimal TyLeThanhToan { get; set; }
    public decimal GiaTriThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
    public bool IsPaid { get; set; }
}

public class TheoDoiHopDongReportRowDto
{
    public int Stt { get; set; }
    public Guid HopDongId { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public string TenHopDong { get; set; } = string.Empty;
    public DateTime? NgayKyHopDong { get; set; }
    public DateTime? NgayKetThucDuKien { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public decimal GiaTriDaThanhToan { get; set; }
    public decimal GiaTriConLai { get; set; }
    public decimal DuKienThanhToanDenMoc { get; set; }
    public string? GhiChu { get; set; }

    public int LoaiHopDong { get; set; }
    public string? LoaiHopDongTen { get; set; }
    public string? TenDuAn { get; set; }
    public string? TenGoiThau { get; set; }
    public string? TenNhaThau { get; set; }

    public List<TheoDoiHopDongDotThanhToanDto> DanhSachDotThanhToan { get; set; } = new();
}

public class TheoDoiHopDongReportSummaryDto
{
    public int TongSoHopDong { get; set; }
    public decimal TongGiaTriHopDong { get; set; }
    public decimal TongGiaTriDaThanhToan { get; set; }
    public decimal TongGiaTriConLai { get; set; }
    public decimal TongDuKienThanhToanDenMoc { get; set; }
}

public class TheoDoiHopDongReportResponseDto
{
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public DateTime CutoffDate { get; set; }
    public int? LoaiHopDong { get; set; }
    public string LoaiHopDongFilterTen { get; set; } = "Tất cả loại hợp đồng";
    public TheoDoiHopDongReportSummaryDto Summary { get; set; } = new();
    public List<TheoDoiHopDongReportRowDto> Rows { get; set; } = new();
}
