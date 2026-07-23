using System;

namespace demo1.DTOs;

public class GoiThauDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public Guid? DuAnId { get; set; }
    public string? DuAnName { get; set; } // Auxiliary for easier UI viewing
    
    public decimal GiaTriGoiThau { get; set; }
    public decimal NguongCanhBaoPercent { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<NhaThauGoiThauDto> NhaThauGoiThaus { get; set; } = new List<NhaThauGoiThauDto>();
}
