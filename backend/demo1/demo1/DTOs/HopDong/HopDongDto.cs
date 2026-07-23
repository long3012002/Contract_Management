using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class HopDongDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // Số ký hiệu
    public string Name { get; set; } = string.Empty; // Tên hợp đồng
    public string? Description { get; set; }

    public Guid? GoiThauId { get; set; }
    public string? GoiThauName { get; set; }

    public Guid? DuAnId { get; set; }
    public string? DuAnName { get; set; }

    public Guid? ChuDauTuId { get; set; }
    public DoiTacDto? ChuDauTu { get; set; }

    public Guid? NhaThauId { get; set; }
    public DoiTacDto? NhaThau { get; set; }

    public int LoaiHopDong { get; set; }
    public string? ThoiHanThucHien { get; set; }
    public string? DiaDiemThucHien { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public int HinhThucThanhToan { get; set; }
    public DateTime? NgayHieuLuc { get; set; }

    public DateTime? ExpiredDate { get; set; }
    public DateTime? RenewalReminderDate { get; set; }
    public bool IsRenewalRequired { get; set; }

    public List<DotThanhToanDto> DotThanhToans { get; set; } = new();
    public List<NhaThauGoiThauDto> NhaThauGoiThaus { get; set; } = new();

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
