using System;

namespace demo1.DTOs
{
    public class RolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public Guid Id => FeatureId;

        public string FeatureCode { get; set; } = string.Empty;
        public string Code => FeatureCode;

        public string FeatureName { get; set; } = string.Empty;
        public string Name => FeatureName;

        public string? Description { get; set; }
        public string? ParentCode { get; set; }
        public int SortOrder { get; set; }
        public bool CanAccess { get; set; }

        public bool CanView => CanAccess || (!string.IsNullOrWhiteSpace(Permissions) && Permissions.ToUpper().Contains("VIEW"));
        public bool CanCreate => !string.IsNullOrWhiteSpace(Permissions) && Permissions.ToUpper().Contains("CREATE");
        public bool CanEdit => !string.IsNullOrWhiteSpace(Permissions) && (Permissions.ToUpper().Contains("EDIT") || Permissions.ToUpper().Contains("UPDATE"));
        public bool CanDelete => !string.IsNullOrWhiteSpace(Permissions) && Permissions.ToUpper().Contains("DELETE");

        public string Permissions { get; set; } = string.Empty;
    }
}
