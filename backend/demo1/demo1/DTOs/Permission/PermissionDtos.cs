using System;
using System.Collections.Generic;

namespace demo1.DTOs.Permission
{
    public class PermissionCatalogDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class PermissionRequestDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public Guid? DuAnId { get; set; }
        public Guid? PermissionId { get; set; }
        public Guid? RequestedPermissionId { get; set; }
        public string RequestedPermissionCode { get; set; } = string.Empty;
        public string RequestedPermissionName { get; set; } = string.Empty;
        public string EntityTitle { get; set; } = string.Empty;
        public string RequestedAction { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePermissionRequestDto
    {
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public Guid? DuAnId { get; set; }
        public Guid? PermissionId { get; set; } // If requesting a specific permission from catalog
        public string EntityTitle { get; set; } = string.Empty;
        public string RequestedAction { get; set; } = "EDIT"; // EDIT, DELETE
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewPermissionRequestDto
    {
        public bool IsApproved { get; set; }
        public string? ReviewNote { get; set; }
    }

    public class UserPermissionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public Guid PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public Guid? DuAnId { get; set; }
        public DateTime GrantedAt { get; set; }
        public string? GrantedByUsername { get; set; }
    }

    public class CreateUserPermissionDto
    {
        public Guid UserId { get; set; }
        public Guid PermissionId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public Guid? DuAnId { get; set; }
    }

    public class DuAnPermissionCheckDto
    {
        public Guid DuAnId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAdmin { get; set; }
        public bool HasPermission { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public List<string> GrantedPermissionCodes { get; set; } = new List<string>();
        public List<Guid> GrantedPermissionIds { get; set; } = new List<Guid>();
        public string? RequestStatus { get; set; } // None, Pending, Approved, Rejected
        public Guid? RequestId { get; set; }
    }
}
