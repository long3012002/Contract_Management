using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class ContractPaymentReportMilestoneDto
{
    public Guid Id { get; set; }
    public string TenDot { get; set; } = string.Empty;
    public decimal TyLeThanhToan { get; set; }
    public decimal GiaTriThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
    public bool IsPaid { get; set; }
}

public class ContractPaymentReportRowDto
{
    public Guid HopDongId { get; set; }
    public string MaHopDong { get; set; } = string.Empty;
    public string TenHopDong { get; set; } = string.Empty;
    public int LoaiHopDong { get; set; }
    public string LoaiHopDongTen { get; set; } = string.Empty;
    public string? TenDuAn { get; set; }
    public string? TenGoiThau { get; set; }
    public string? TenNhaThau { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public DateTime? NgayHieuLuc { get; set; }
    public DateTime? ExpiredDate { get; set; }

    // Milestones count
    public int TongSoKy { get; set; }
    public int SoKyDaThanhToan { get; set; }
    public int SoKyConPhaiThanhToan { get; set; }

    // Money overall contract
    public decimal SoTienDaThanhToan { get; set; }
    public decimal SoTienConPhaiThanhToan { get; set; }
    public double TyLeDaThanhToanPercent { get; set; }

    // Money in selected year
    public decimal PhaiThanhToanTrongNam { get; set; }
    public decimal DaThanhToanTrongNam { get; set; }
    public decimal ConPhaiThanhToanTrongNam { get; set; }

    // Payment Status
    public string TrangThaiThanhToan { get; set; } = string.Empty;

    // Milestone Details
    public List<ContractPaymentReportMilestoneDto> DanhSachDotThanhToan { get; set; } = new();
}

public class ContractPaymentReportSummaryDto
{
    public int TongSoHopDong { get; set; }
    public int SoHopDongBaoTri { get; set; }
    public decimal TongGiaTriHopDong { get; set; }
    public int TongSoKyThanhToan { get; set; }
    public int TongSoKyDaThanhToan { get; set; }
    public int TongSoKyConPhaiThanhToan { get; set; }
    public decimal TongSoTienDaThanhToan { get; set; }
    public decimal TongSoTienConPhaiThanhToan { get; set; }
    public decimal TongPhaiThanhToanTrongNam { get; set; }
    public decimal TongDaThanhToanTrongNam { get; set; }
    public decimal TongConPhaiThanhToanTrongNam { get; set; }
}

public class ContractPaymentReportResponseDto
{
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? LoaiHopDong { get; set; }
    public string LoaiHopDongFilterTen { get; set; } = "Tất cả loại hợp đồng";
    public ContractPaymentReportSummaryDto Summary { get; set; } = new();
    public List<ContractPaymentReportRowDto> Rows { get; set; } = new();
}
