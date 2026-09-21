using System;
using System.Collections.Generic;

namespace demo1.Entity;

/// <summary>
/// Quản lý các đợt/văn bản Kế hoạch vốn dài hạn, hàng năm hoặc bổ sung.
/// </summary>
public class KeHoachVon : BaseEntity
{
    public int NamKeHoach { get; set; }
    
    /// <summary>
    /// 1 = 6 tháng đầu năm, 2 = Cả năm, 3 = Bổ sung
    /// </summary>
    public int LoaiKeHoach { get; set; }
    
    /// <summary>
    /// Thứ tự đợt bổ sung (khi LoaiKeHoach = 3)
    /// </summary>
    public int? DotBoSung { get; set; }
    
    /// <summary>
    /// 1 = Draft, 2 = Submitted, 3 = Approved, 4 = Rejected
    /// </summary>
    public int TrangThai { get; set; } = 1;

    public string? SoQuyetDinh { get; set; }
    public DateTime? NgayPheDuyet { get; set; }

    public decimal TongMucDeNghi { get; set; }
    public decimal TongMucDuocDuyet { get; set; }

    public string? GhiChu { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<KeHoachVonDuAn> KeHoachVonDuAns { get; set; } = new List<KeHoachVonDuAn>();
}
