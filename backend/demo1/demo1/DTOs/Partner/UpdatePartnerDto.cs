using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class UpdatePartnerDto
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}
