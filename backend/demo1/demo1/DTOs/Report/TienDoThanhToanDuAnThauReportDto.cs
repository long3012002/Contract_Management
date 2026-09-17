using System;
using System.Collections.Generic;

namespace demo1.DTOs;

/// <summary>
/// DTO chứa dữ liệu 1 dòng cho Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu (Mẫu Excel).
/// </summary>
public class TienDoThanhToanDuAnThauReportRowDto
{
    public int Stt { get; set; }
    public string SttDisplay { get; set; } = string.Empty;
    public string NguonVon { get; set; } = string.Empty;
    public string TenDuAn { get; set; } = string.Empty;
    public string? SoQuyetDinhPheDuyetDuToan { get; set; }
    public string TenGoiThau { get; set; } = string.Empty;
    public string? SoQuyetDinhKQLCNT { get; set; }
    public string TenNhaThau { get; set; } = string.Empty;
    public string? MaSoThue { get; set; }
    public string? DiaChi { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public DateTime? NgayKy { get; set; }
    public string? ThoiGianThucHien { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public decimal TamUng { get; set; }
    
    /// <summary>
    /// Danh sách các giá trị đợt thanh toán (Lần 1, Lần 2, Lần 3...)
    /// </summary>
    public List<decimal> CacLanThanhToan { get; set; } = new();

    public string? GhiChu { get; set; }

    public Guid HopDongId { get; set; }
    public Guid? GoiThauId { get; set; }
    public Guid? DuAnId { get; set; }
}

/// <summary>
/// DTO Tổng hợp thông tin báo cáo.
/// </summary>
public class TienDoThanhToanDuAnThauReportSummaryDto
{
    public int TongSoHopDong { get; set; }
    public decimal TongGiaTriHopDong { get; set; }
    public decimal TongTamUng { get; set; }
    public decimal TongDaThanhToan { get; set; }

    /// <summary>
    /// Tổng giá trị cho từng đợt thanh toán (Lần 1, Lần 2, Lần 3...) dùng hiển thị Footer
    /// </summary>
    public List<decimal> TongCacLanThanhToan { get; set; } = new();

    /// <summary>
    /// Alias field sumCacLanThanhToan cho FE
    /// </summary>
    public List<decimal> SumCacLanThanhToan => TongCacLanThanhToan;
}

/// <summary>
/// DTO Phản hồi tổng thể cho Báo cáo Theo dõi Tiến độ Thanh toán các Dự án Thầu.
/// </summary>
public class TienDoThanhToanDuAnThauReportResponseDto
{
    public string Title { get; set; } = "BÁO CÁO THEO DÕI TIẾN ĐỘ THANH TOÁN CÁC DỰ ÁN THẦU";
    public string Unit { get; set; } = "Đồng";
    public int MaxDotThanhToanCount { get; set; } = 3;
    public TienDoThanhToanDuAnThauReportSummaryDto Summary { get; set; } = new();
    public List<TienDoThanhToanDuAnThauReportRowDto> Rows { get; set; } = new();
}
