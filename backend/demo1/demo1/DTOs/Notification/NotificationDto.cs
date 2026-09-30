using System;

namespace demo1.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Thông tin nội dung tối giản cho Frontend render
    public string? Category { get; set; }
    public string? ActorName { get; set; }
    public string? Message { get; set; }
    public string? TargetName { get; set; }
}
