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

    public string? SoQuyetDinhKHLCNT { get; set; }
    public DateTime? NgayPheDuyetKHLCNT { get; set; }
    public string? SoQuyetDinhKQLCNT { get; set; }
    public DateTime? NgayPheDuyetKQLCNT { get; set; }

    /// <summary>
    /// Hình thức lựa chọn nhà thầu (Đấu thầu rộng rãi qua mạng, Chỉ định thầu...)
    /// </summary>
    [StringLength(255)]
    public string? HinhThucLcnt { get; set; }

    /// <summary>
    /// Phương thức lựa chọn nhà thầu (1 GĐ 1 THS, 1 GĐ 2 THS...)
    /// </summary>
    [StringLength(255)]
    public string? PhuongThucLcnt { get; set; }
}
