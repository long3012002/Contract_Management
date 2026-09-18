using System;

namespace demo1.DTOs
{
    public class UpdateRolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; }
        public bool? CanView { get; set; }
        public bool? CanCreate { get; set; }
        public bool? CanEdit { get; set; }
        public bool? CanDelete { get; set; }
        public string? Permissions { get; set; } = string.Empty;
    }
}
