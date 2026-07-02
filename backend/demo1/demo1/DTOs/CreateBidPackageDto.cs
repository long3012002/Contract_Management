using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateBidPackageDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public Guid? ProjectId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal EstimatedValue { get; set; }

    [Range(1, 100)]
    public decimal WarningThresholdPercent { get; set; } = 100;
}
