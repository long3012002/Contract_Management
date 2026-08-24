using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class CreateLoaiHopDongDto
{
    [Required(ErrorMessage = "Code is required")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
