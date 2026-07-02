using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateResolutionDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Summary { get; set; }

    public DateTime? IssuedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }

    [StringLength(500)]
    public string? FileUrl { get; set; }
}
