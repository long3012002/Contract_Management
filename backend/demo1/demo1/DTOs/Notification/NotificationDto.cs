using System;

namespace demo1.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Link { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Structured UI Metadata (Hỗ trợ Frontend hiển thị phẳng)
    public string? Category { get; set; }
    public string? ActorName { get; set; }
    public string? ActionBadgeText { get; set; }
    public string? ActionBadgeVariant { get; set; }
    public string? Message { get; set; }
    public string? TargetName { get; set; }
}
