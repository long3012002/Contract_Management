using System;

namespace demo1.DTOs;

public class NhaThauGoiThauDto
{
    public Guid Id { get; set; }
    public Guid HopDongId { get; set; }
    public Guid NhaThauId { get; set; }
    public string? NhaThauName { get; set; }
    public string? NhaThauCode { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Account { get; set; }
    public string? Representative { get; set; }
    public string? Position { get; set; }

    public bool IsLienDanh { get; set; }
}
