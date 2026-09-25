using System;

namespace demo1.DTOs;

public class ResolutionDto : IHasId
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime? IssuedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? FileUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
