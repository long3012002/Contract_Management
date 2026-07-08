using System;

namespace demo1.DTOs
{
    public class UpdateRolePermissionDto
    {
        public Guid FeatureId { get; set; }
        public bool CanAccess { get; set; }
        public string Permissions { get; set; } = string.Empty;
    }
}
