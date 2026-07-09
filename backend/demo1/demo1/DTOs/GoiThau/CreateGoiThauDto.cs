using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateGoiThauDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public Guid? DuAnId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal GiaTriGoiThau { get; set; }

    [Range(0, 100)]
    public decimal NguongCanhBaoPercent { get; set; } = 100;
}
