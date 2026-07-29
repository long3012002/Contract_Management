using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

/// <summary>
/// DTO Tạo mới Hợp đồng kèm danh sách Đợt thanh toán.
/// </summary>
public class CreateHopDongDto
{
    /// <summary>
    /// Số hiệu / Ký hiệu hợp đồng
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Tên hợp đồng
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả chi tiết nội dung hợp đồng
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Mã Gói thầu liên quan (GUID)
    /// </summary>
    public Guid? GoiThauId { get; set; }

    /// <summary>
    /// Mã Dự án liên quan (GUID)
    /// </summary>
    public Guid? DuAnId { get; set; }

    /// <summary>
    /// Mã Đơn vị Chủ đầu tư (GUID)
    /// </summary>
    public Guid? ChuDauTuId { get; set; }

    /// <summary>
    /// Mã Nhà thầu / Đối tác thực hiện (GUID)
    /// </summary>
    public Guid? NhaThauId { get; set; }

    /// <summary>
    /// Phân loại hợp đồng (1: Mua sắm hàng hóa, 2: Xây lắp, 3: Dịch vụ tư vấn, 4: Dịch vụ phi tư vấn...)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int LoaiHopDong { get; set; }

    /// <summary>
    /// Thời hạn thực hiện hợp đồng
    /// </summary>
    [StringLength(255)]
    public string? ThoiHanThucHien { get; set; }

    /// <summary>
    /// Địa điểm thực hiện hợp đồng
    /// </summary>
    [StringLength(500)]
    public string? DiaDiemThucHien { get; set; }

    /// <summary>
    /// Giá trị hợp đồng (VNĐ)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal GiaTriHopDong { get; set; }

    /// <summary>
    /// Hình thức thanh toán (1: Tiền mặt, 2: Chuyển khoản)
    /// </summary>
    [Range(1, 2)]
    public int HinhThucThanhToan { get; set; }

    /// <summary>
    /// Ngày hợp đồng có hiệu lực
    /// </summary>
    public DateTime? NgayHieuLuc { get; set; }

    /// <summary>
    /// Ngày hợp đồng hết hạn
    /// </summary>
    public DateTime? ExpiredDate { get; set; }

    /// <summary>
    /// Ngày nhắc nhở gia hạn hợp đồng
    /// </summary>
    public DateTime? RenewalReminderDate { get; set; }

    /// <summary>
    /// Cờ yêu cầu gia hạn khi sắp hết hạn (mặc định: true)
    /// </summary>
    public bool IsRenewalRequired { get; set; } = true;

    /// <summary>
    /// Danh sách các đợt thanh toán theo kế hoạch của hợp đồng
    /// </summary>
    public List<CreateDotThanhToanDto> DotThanhToans { get; set; } = new();

    /// <summary>
    /// Danh sách nhà thầu liên kết
    /// </summary>
    public List<NhaThauGoiThauInputDto>? NhaThauGoiThaus { get; set; } = new();
}
