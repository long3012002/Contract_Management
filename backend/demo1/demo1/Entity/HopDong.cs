using System;
using System.Collections.Generic;

namespace demo1.Entity;

public class HopDong : BaseEntity
{
    public Guid? GoiThauId { get; set; }
    public virtual GoiThau? GoiThau { get; set; }

    public Guid? DuAnId { get; set; }
    public virtual DuAn? DuAn { get; set; }

    public Guid? ChuDauTuId { get; set; }
    public virtual DoiTac? ChuDauTu { get; set; }

    public Guid? NhaThauId { get; set; }
    public virtual DoiTac? NhaThau { get; set; }

    public int LoaiHopDong { get; set; }
    public string? ThoiHanThucHien { get; set; }
    public string? DiaDiemThucHien { get; set; }
    public decimal GiaTriHopDong { get; set; }
    public int HinhThucThanhToan { get; set; } // 1. Tiền mặt / 2. Chuyển khoản
    public DateTime? NgayHieuLuc { get; set; }
    
    // Auxiliary fields to support contract warning services
    public DateTime? ExpiredDate { get; set; }
    public DateTime? RenewalReminderDate { get; set; }
    public bool IsRenewalRequired { get; set; } = true;

    public virtual ICollection<DotThanhToan> DotThanhToans { get; set; } = new List<DotThanhToan>();
    public virtual ICollection<NhaThauGoiThau> NhaThauGoiThaus { get; set; } = new List<NhaThauGoiThau>();
}
