using System;

namespace demo1.DTOs;

public class DotThanhToanReportDto
{
    public Guid Id { get; set; }
    public string TenDot { get; set; } = string.Empty;
    public decimal TyLeThanhToan { get; set; }
    public decimal GiaTriThanhToan { get; set; }
    public DateTime? NgayThanhToan { get; set; }
    public string? DieuKienThanhToan { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Associated Contract
    public Guid HopDongId { get; set; }
    public string HopDongCode { get; set; } = string.Empty;
    public string HopDongName { get; set; } = string.Empty;

    // Associated Bidding Package
    public Guid? GoiThauId { get; set; }
    public string? GoiThauCode { get; set; }
    public string? GoiThauName { get; set; }

    // Associated Project
    public Guid? DuAnId { get; set; }
    public string? DuAnCode { get; set; }
    public string? DuAnName { get; set; }
}
