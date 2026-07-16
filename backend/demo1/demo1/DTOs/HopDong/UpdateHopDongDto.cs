using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class UpdateHopDongDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty; // Số ký hiệu

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty; // Tên hợp đồng

    [StringLength(1000)]
    public string? Description { get; set; }

    public Guid? GoiThauId { get; set; }
    public Guid? DuAnId { get; set; }
    public Guid? ChuDauTuId { get; set; }
    public Guid? NhaThauId { get; set; }

    [Range(1, int.MaxValue)]
    public int LoaiHopDong { get; set; }

    [StringLength(255)]
    public string? ThoiHanThucHien { get; set; }

    [StringLength(500)]
    public string? DiaDiemThucHien { get; set; }

    [Range(0, double.MaxValue)]
    public decimal GiaTriHopDong { get; set; }

    [Range(1, 2)]
    public int HinhThucThanhToan { get; set; } // 1. Tiền mặt / 2. Chuyển khoản

    public DateTime? NgayHieuLuc { get; set; }

    public DateTime? ExpiredDate { get; set; }
    public DateTime? RenewalReminderDate { get; set; }
    public bool IsRenewalRequired { get; set; } = true;

    public List<CreateDotThanhToanDto> DotThanhToans { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
