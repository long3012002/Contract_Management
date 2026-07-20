using System;

namespace demo1.Entity
{
    public class UserPermission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public Guid PermissionId { get; set; }
        public Guid? DuAnId { get; set; }
        public string FeatureCode { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public Guid? GrantedByUserId { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public User? GrantedByUser { get; set; }
        public Permission? Permission { get; set; }
        public DuAn? DuAn { get; set; }
    }
}
