using System;
using System.Collections.Generic;

namespace demo1.DTOs;

/// <summary>
/// DTO dòng báo cáo tổng hợp Kế hoạch &amp; Kết quả Lựa chọn Nhà thầu (Mẫu Báo cáo 3 Excel)
/// </summary>
public class GoiThauLcntReportRowDto
{
    public int Stt { get; set; }
    public Guid GoiThauId { get; set; }
    public Guid? DuAnId { get; set; }
    public string MaDuAn { get; set; } = string.Empty;
    public string TenDuAn { get; set; } = string.Empty;
    public string MaGoiThau { get; set; } = string.Empty;
    public string TenGoiThau { get; set; } = string.Empty;
    public decimal GiaTriDuToan { get; set; }
    public string HinhThucLcnt { get; set; } = string.Empty;
    public string PhuongThucLcnt { get; set; } = string.Empty;
    public decimal TongGiaTriHopDongDaKy { get; set; }
    public decimal GiaTriTietKiem { get; set; }
    public double TyLeSuDungDuToanPercent { get; set; }
    public string TenNhaThauTrungThau { get; set; } = string.Empty;
    public string TrangThaiGoiThau { get; set; } = string.Empty;
}

public class GoiThauLcntReportSummaryDto
{
    public int TongSoGoiThau { get; set; }
    public decimal TongGiaTriDuToan { get; set; }
    public decimal TongGiaTriHopDongDaKy { get; set; }
    public decimal TongGiaTriTietKiem { get; set; }
    public double TyLeTietKiemChungPercent { get; set; }
}

public class GoiThauLcntReportResponseDto
{
    public string Title { get; set; } = "BÁO CÁO KẾ HOẠCH & KẾT QUẢ LỰA CHỌN NHÀ THẦU";
    public string Unit { get; set; } = "Đồng";
    public int? Year { get; set; }
    public GoiThauLcntReportSummaryDto Summary { get; set; } = new();
    public List<GoiThauLcntReportRowDto> Rows { get; set; } = new();
}
