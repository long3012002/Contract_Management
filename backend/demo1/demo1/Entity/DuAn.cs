using System;
using System.Collections.Generic;
using demo1.Entity.DanhMuc;

namespace demo1.Entity;

public class DuAn : BaseEntity
{
    public decimal DuToanPheDuyet { get; set; }
    public int TrangThai { get; set; } = 1;
    
    // 1 = Du An Nguon, 2 = Du An Trien Khai
    public int LoaiDuAn { get; set; }

    public Guid? NhomDuAnId { get; set; }
    public virtual NhomDuAn? NhomDuAn { get; set; }

    public Guid? PhanLoaiDuAnId { get; set; }
    public virtual PhanLoaiDuAn? PhanLoaiDuAn { get; set; }

    // Navigation Properties cho quan hệ giữa Dự án Triển khai và Dự án Nguồn
    public virtual ICollection<DuAnNguonTrienKhai> NguonDuAns { get; set; } = new List<DuAnNguonTrienKhai>();
    
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

    public Guid? CreatedByUserId { get; set; }
    public virtual User? CreatedByUser { get; set; }

    public Guid? ChuDuAnId { get; set; }
    public virtual User? ChuDuAn { get; set; }

    public virtual ICollection<DieuChinhDuAn> DieuChinhs { get; set; } = new List<DieuChinhDuAn>();
    public virtual ICollection<GoiThau> GoiThaus { get; set; } = new List<GoiThau>();
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    public virtual ICollection<DuAnPhanKyVon> PhanKyVons { get; set; } = new List<DuAnPhanKyVon>();
    public virtual ICollection<DuAnNguonVon> DanhSachNguonVon { get; set; } = new List<DuAnNguonVon>();
}
