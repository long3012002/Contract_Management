using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateProjectDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal TotalBudget { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Planning";
}
