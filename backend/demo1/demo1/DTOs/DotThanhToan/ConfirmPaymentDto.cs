using System;

namespace demo1.DTOs;

/// <summary>
/// DTO Xác nhận hoàn tất thanh toán đợt thanh toán
/// </summary>
public class ConfirmPaymentDto
{
    /// <summary>
    /// Ngày thực hiện thanh toán thực tế (nếu không truyền mặc định lấy thời gian hiện tại)
    /// </summary>
    public DateTime? NgayThanhToanThucTe { get; set; }

    /// <summary>
    /// Ghi chú / Số ủy nhiệm chi / Chứng từ thanh toán thực tế
    /// </summary>
    public string? GhiChuThanhToan { get; set; }
}
