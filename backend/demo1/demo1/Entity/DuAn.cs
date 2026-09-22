using System;
using System.Collections.Generic;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class DuAn : BaseEntity
{
    public decimal DuToanPheDuyet { get; set; }
    public int TrangThai { get; set; } = 3;

    public Guid? NhomDuAnId { get; set; }
    public virtual NhomDuAn? NhomDuAn { get; set; }

    public Guid? PhanLoaiDuAnId { get; set; }
    public virtual PhanLoaiDuAn? PhanLoaiDuAn { get; set; }

    public string? ChuDauTu { get; set; }
    public string? DiaDiemThucHien { get; set; }
    public string? ThoiGianThucHien { get; set; }
    
    public string? NoiDung { get; set; }
    public int? HinhThucQuanLy { get; set; }
    public string? ToChucThucHien { get; set; }
    
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public DateTime? NgayKetThucThucTe { get; set; }
    public int? NamBatDau { get; set; }
    public int? NamKetThuc { get; set; }
    public bool DaKetThuc { get; set; } = false;
    public bool? DaTrienKhai { get; set; }
    public string? SoQuyetDinh { get; set; }
    public string? SoQuyetDinhPheDuyetDuToan { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public Guid? ChuDuAnId { get; set; }
    public virtual User? ChuDuAn { get; set; }

    // Navigation properties cho Kế hoạch vốn và Gộp dự án
    public virtual ICollection<KeHoachVonDuAn> KeHoachVonDuAns { get; set; } = new List<KeHoachVonDuAn>();
    public virtual ICollection<DuAnGopLink> MergedFromGopLinks { get; set; } = new List<DuAnGopLink>();
    public virtual DuAnGopLink? MergedToGopLink { get; set; }

    public virtual ICollection<GoiThau> GoiThaus { get; set; } = new List<GoiThau>();
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    public virtual ICollection<DuAnPhanKyVon> PhanKyVons { get; set; } = new List<DuAnPhanKyVon>();
    public virtual ICollection<DuAnNguonVon> DanhSachNguonVon { get; set; } = new List<DuAnNguonVon>();
}
