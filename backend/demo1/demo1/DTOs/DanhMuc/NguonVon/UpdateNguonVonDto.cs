using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class UpdateNguonVonDto
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
