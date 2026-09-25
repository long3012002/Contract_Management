using System;

namespace demo1.DTOs;

public class DuAnKeHoachVonDto
{
    public Guid KeHoachVonId { get; set; }
    public string? SoQuyetDinh { get; set; }
    public string? TenKeHoachVon { get; set; }
    public int NamKeHoach { get; set; }
    public int LoaiKeHoach { get; set; }
    public string? LoaiKeHoachText { get; set; }
    public int TrangThai { get; set; }
    public string? TrangThaiText { get; set; }
    public decimal SoTienDeNghi { get; set; }
    public decimal SoTienDuocDuyet { get; set; }
    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }
    public string? GhiChu { get; set; }
}

public class CreateDuAnKeHoachVonDto
{
    public Guid KeHoachVonId { get; set; }
    public decimal? SoTienDeNghi { get; set; }
    public decimal? SoTienDuocDuyet { get; set; }
    public decimal? VonDieuLe { get; set; }
    public decimal? QuyDauTuPhatTrien { get; set; }
    public string? GhiChu { get; set; }
}
