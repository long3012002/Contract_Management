using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace demo1.Data
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public Dictionary<string, object> KeyValues { get; } = new();
        public Dictionary<string, object> OldValues { get; } = new();
        public Dictionary<string, object> NewValues { get; } = new();
        public List<string> ChangedColumns { get; } = new();
        public string? IpAddress { get; set; }

        public Entity.AuditLog ToAuditLog()
        {
            return new Entity.AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                Username = Username,
                Action = Action,
                TableName = TableName,
                EntityId = KeyValues.Count == 1 ? KeyValues.Values.First().ToString()! : JsonSerializer.Serialize(KeyValues),
                OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
                NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
                ChangedColumns = ChangedColumns.Count == 0 ? null : JsonSerializer.Serialize(ChangedColumns),
                Timestamp = DateTime.UtcNow,
                IpAddress = IpAddress
            };
        }
    }
}
