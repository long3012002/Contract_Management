using System;
using System.Collections.Generic;

namespace demo1.Entity;

public class DuAn : BaseEntity
{
    public decimal DuToanPheDuyet { get; set; }
    public int TrangThai { get; set; } = 1;
    
    // 1 = Du An Nguon, 2 = Du An Trien Khai
    public int LoaiDuAn { get; set; }
    
    // Semicolon separated list of source project GUIDs (e.g. "guid1;guid2;guid3")
    public string? NguonDuAnIds { get; set; }
    
    public string? ChuDauTu { get; set; }
    public string? DiaDiemThucHien { get; set; }
    public string? ThoiGianThucHien { get; set; }
    
    public string? NoiDung { get; set; }
    public int? HinhThucQuanLy { get; set; }
    public string? ToChucThucHien { get; set; }
    
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public int? NamBatDau { get; set; }
    public int? NamKetThuc { get; set; }
    public bool DaKetThuc { get; set; } = false;
    public bool? DaTrienKhai { get; set; }
    public string? SoQuyetDinh { get; set; }

    public virtual ICollection<DieuChinhDuAn> DieuChinhs { get; set; } = new List<DieuChinhDuAn>();
    public virtual ICollection<GoiThau> GoiThaus { get; set; } = new List<GoiThau>();
}
