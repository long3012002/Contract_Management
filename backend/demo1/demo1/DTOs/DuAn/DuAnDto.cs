using System;
using System.Collections.Generic;
using System.Linq;

namespace demo1.DTOs;

public class DuAnDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DuToanPheDuyet { get; set; }
    
    // Computed value: DuToanPheDuyet + adjustments
    public decimal TongDuToanHienTai { get; set; }
    
    public int TrangThai { get; set; }
    public int LoaiDuAn { get; set; }
    public string? NguonDuAnIds { get; set; }
    
    public List<Guid> ListNguonDuAnIds
    {
        get
        {
            if (string.IsNullOrWhiteSpace(NguonDuAnIds))
                return new List<Guid>();
            return NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                               .Where(g => g != Guid.Empty)
                               .ToList();
        }
    }
    
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
    public bool DaKetThuc { get; set; }
    public bool? DaTrienKhai { get; set; }
    public string? SoQuyetDinh { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
