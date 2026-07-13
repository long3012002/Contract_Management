using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateDoiTacDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(255)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Account { get; set; }

    [StringLength(255)]
    public string? Representative { get; set; }

    [StringLength(255)]
    public string? Position { get; set; }
}
