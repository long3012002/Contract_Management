using System;

namespace demo1.Entity
{
    public class RolePermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RoleId { get; set; }
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; } = true;
        public string Permissions { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Role? Role { get; set; }
        public Feature? Feature { get; set; }
    }
}
