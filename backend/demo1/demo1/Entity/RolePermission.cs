using System;

namespace demo1.Entity
{
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; } = false;
        public bool CanCreate { get; set; } = false;
        public bool CanUpdate { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Role? Role { get; set; }
        public Feature? Feature { get; set; }
    }
}
