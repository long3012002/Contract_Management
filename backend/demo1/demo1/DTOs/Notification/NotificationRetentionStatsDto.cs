using System;

namespace demo1.DTOs.Notification
{
    public class NotificationRetentionStatsDto
    {
        public int TotalNotifications { get; set; }
        public int TotalRead { get; set; }
        public int TotalUnread { get; set; }
        public int ReadOlderThan14Days { get; set; }
        public int ReadOlderThan30Days { get; set; }
        public int ReadOlderThan60Days { get; set; }
    }
}
