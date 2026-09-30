using System;

namespace demo1.DTOs.Notification
{
    public class AdminNotificationCleanupRequestDto
    {
        public int ReadRetentionDays { get; set; } = 14;
        public bool DryRun { get; set; } = false;
    }
}
