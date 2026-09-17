using System;

namespace demo1.DTOs
{
    public class RolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string FeatureName { get; set; } = string.Empty;
        public string? ParentCode { get; set; }
        public int SortOrder { get; set; }
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }
}
