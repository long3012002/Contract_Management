using System;

namespace demo1.DTOs;

public class DuAnNguonVonDto
{
    public Guid Id { get; set; }
    public Guid DuAnId { get; set; }
    public Guid NguonVonId { get; set; }
    public string? MaNguonVon { get; set; }
    public string? TenNguonVon { get; set; }
    public decimal SoTien { get; set; }
    public string? GhiChu { get; set; }
}

public class CreateDuAnNguonVonDto
{
    public Guid NguonVonId { get; set; }
    public decimal SoTien { get; set; }
    public string? GhiChu { get; set; }
}
