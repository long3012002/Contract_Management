using System;

namespace demo1.DTOs
{
    public class ChucVuDto
    {
        public Guid Id { get; set; }
        public string TenChucVu { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateChucVuDto
    {
        public string TenChucVu { get; set; } = string.Empty;
    }

    public class UpdateChucVuDto
    {
        public string TenChucVu { get; set; } = string.Empty;
    }

    public class ChucVuPermissionDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }

    public class UpdateChucVuPermissionDto
    {
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }
}
