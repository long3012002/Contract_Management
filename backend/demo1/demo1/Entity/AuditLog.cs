using System;

namespace demo1.Entity
{
    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE
        public string TableName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string? OldValues { get; set; } // JSON string
        public string? NewValues { get; set; } // JSON string
        public string? ChangedColumns { get; set; } // JSON string
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? IpAddress { get; set; }
    }
}
