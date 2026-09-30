using System;
using System.Collections.Generic;

namespace demo1.DTOs.Notification
{
    public class BulkDeleteNotificationDto
    {
        public List<Guid> Ids { get; set; } = new();
    }
}
