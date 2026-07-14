using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class UpdateDuAnDto
{
    public List<Guid>? SourceProjectIds { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal DuToanPheDuyet { get; set; }

    public int TrangThai { get; set; }

    [StringLength(255)]
    public string? ChuDauTu { get; set; }

    [StringLength(500)]
    public string? DiaDiemThucHien { get; set; }

    [StringLength(255)]
    public string? ThoiGianThucHien { get; set; }

    public string? NoiDung { get; set; }
    public int? HinhThucQuanLy { get; set; }

    [StringLength(500)]
    public string? ToChucThucHien { get; set; }

    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public int? NamBatDau { get; set; }
    public int? NamKetThuc { get; set; }
    public bool DaKetThuc { get; set; }

    public string? SoQuyetDinh { get; set; }
}
