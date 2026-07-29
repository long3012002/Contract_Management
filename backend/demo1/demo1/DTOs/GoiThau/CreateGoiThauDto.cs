using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

/// <summary>
/// DTO Tạo mới Gói thầu.
/// </summary>
public class CreateGoiThauDto
{
    /// <summary>
    /// Mã Gói thầu
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên Gói thầu
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết gói thầu
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Mã Dự án liên quan (GUID)
    /// </summary>
    public Guid? DuAnId { get; set; }

    /// <summary>
    /// Giá trị dự toán gói thầu (VNĐ)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal GiaTriGoiThau { get; set; }

    /// <summary>
    /// Ngưỡng cảnh báo kinh phí (phần trăm %, mặc định 100%)
    /// </summary>
    [Range(0, 100)]
    public decimal NguongCanhBaoPercent { get; set; } = 100;
}
