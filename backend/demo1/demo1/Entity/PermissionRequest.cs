using System;

namespace demo1.Entity
{
    public class PermissionRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid? DuAnId { get; set; }
        public Guid? PermissionId { get; set; }
        public Guid? RequestedPermissionId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityTitle { get; set; } = string.Empty;
        public string RequestedAction { get; set; } = "EDIT"; // EDIT, DELETE
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public Guid? ReviewerId { get; set; }
        public string? ReviewNote { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public User? Reviewer { get; set; }
        public DuAn? DuAn { get; set; }
        public UserPermission? Permission { get; set; }
        public Permission? RequestedPermission { get; set; }
    }
}
