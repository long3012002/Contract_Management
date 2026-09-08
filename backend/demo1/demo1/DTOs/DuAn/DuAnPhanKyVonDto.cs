using System;

namespace demo1.DTOs;

public class DuAnPhanKyVonDto
{
    public Guid? Id { get; set; }
    public Guid? DuAnId { get; set; }
    public int Nam { get; set; }
    public decimal SoTienPhanKy { get; set; }
    public decimal? TyLePercent { get; set; }
    public string? GhiChu { get; set; }
}

public class CreateDuAnPhanKyVonDto
{
    public int Nam { get; set; }
    public decimal SoTienPhanKy { get; set; }
    public decimal? TyLePercent { get; set; }
    public string? GhiChu { get; set; }
}
