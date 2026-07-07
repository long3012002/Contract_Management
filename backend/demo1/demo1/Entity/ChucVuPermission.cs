using System;

namespace demo1.Entity
{
    public class ChucVuPermission
    {
        public Guid ChucVuId { get; set; }
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; } = false;
        public string Permissions { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ChucVu? ChucVu { get; set; }
        public Feature? Feature { get; set; }
    }
}
