using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class UpdateLoaiHopDongDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
}
