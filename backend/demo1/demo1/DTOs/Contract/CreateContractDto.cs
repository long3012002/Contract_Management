using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateContractDto
{
    [Required]
    [StringLength(50)]
    public string ContractNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public Guid? DuAnId { get; set; }
    public Guid? GoiThauId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ContractValue { get; set; }

    public DateTime? SignedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public DateTime? RenewalReminderDate { get; set; }
    public bool IsRenewalRequired { get; set; } = true;

    [StringLength(50)]
    public string Status { get; set; } = "Draft";
}
