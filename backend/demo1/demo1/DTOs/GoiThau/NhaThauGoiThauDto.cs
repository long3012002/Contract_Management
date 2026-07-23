using System;

namespace demo1.DTOs;

public class NhaThauGoiThauDto
{
    public Guid Id { get; set; }
    public Guid HopDongId { get; set; }
    public Guid NhaThauId { get; set; }
    public string? NhaThauName { get; set; }
    public string? NhaThauCode { get; set; }

    public bool IsLienDanh { get; set; }

}
