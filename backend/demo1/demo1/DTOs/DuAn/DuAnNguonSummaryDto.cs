using System;

namespace demo1.DTOs;

/// <summary>
/// DTO Tóm tắt thông tin cơ bản của Dự án Nguồn (Nguồn vốn/Dự án mua sắm)
/// </summary>
public class DuAnNguonSummaryDto : IHasId
{
    /// <summary>
    /// Mã định danh duy nhất của Dự án Nguồn (GUID)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Mã số dự án
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên dự án nguồn
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Dự toán phê duyệt ban đầu (VNĐ)
    /// </summary>
    public decimal DuToanPheDuyet { get; set; }

    /// <summary>
    /// Tổng dự toán hiện tại sau khi đã cộng/trừ các đợt điều chỉnh (VNĐ)
    /// </summary>
    public decimal TongDuToanHienTai { get; set; }
}
