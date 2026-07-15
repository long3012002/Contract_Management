using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class ReportRowDto
{
    public string Stt { get; set; } = string.Empty;
    public string RowType { get; set; } = string.Empty; // GroupHeader, SubGroupHeader, ProjectRow, GroupFooter, GrandTotal
    public string ProjectName { get; set; } = string.Empty;
    public string ApprovalDecision { get; set; } = string.Empty;
    
    // Tổng mức vốn đầu tư
    public decimal TongMucDauTuTong { get; set; }
    public decimal TongMucDauTuVCSH { get; set; }
    public decimal TongMucDauTuVay { get; set; }
    public decimal TongMucDauTuKhac { get; set; }
    
    // Giá trị khối lượng thực hiện
    public decimal KhoiLuongKyTruoc { get; set; }
    public decimal KhoiLuongTrongKy { get; set; }
    public decimal KhoiLuongLuyKe { get; set; }
    
    // Giải ngân
    public decimal GiaiNganKyTruoc { get; set; }
    public decimal GiaiNganTrongKy { get; set; }
    public decimal GiaiNganLuyKe { get; set; }
    
    // Giá trị tài sản đã hoàn thành đưa vào sử dụng
    public decimal TaiSanBanGiao { get; set; }
}

public class ReportResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string Unit { get; set; } = "Triệu đồng";
    public int Year { get; set; }
    public int Period { get; set; } // 1: 6T, 2: 1N
    public string PeriodName { get; set; } = string.Empty; // "6T" or "1N"
    public List<ReportRowDto> Rows { get; set; } = new();
}
