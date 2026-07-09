using System;

namespace demo1.DTOs
{
    public class PhongBanDto
    {
        public Guid Id { get; set; }
        public string TenPhongBan { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePhongBanDto
    {
        public string TenPhongBan { get; set; } = string.Empty;
    }

    public class UpdatePhongBanDto
    {
        public string TenPhongBan { get; set; } = string.Empty;
    }

    public class PhongBanPermissionDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }

    public class UpdatePhongBanPermissionDto
    {
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }
}
