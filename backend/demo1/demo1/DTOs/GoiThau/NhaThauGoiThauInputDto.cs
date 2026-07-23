using System;
using System.ComponentModel.DataAnnotations;

namespace demo1.DTOs;

public class NhaThauGoiThauInputDto
{
    [Required]
    public Guid NhaThauId { get; set; }

    public bool IsLienDanh { get; set; }

}
