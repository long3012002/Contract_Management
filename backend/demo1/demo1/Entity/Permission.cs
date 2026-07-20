using System;

namespace demo1.Entity
{
    public class Permission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Code { get; set; } = string.Empty; // e.g. "VIEW", "CREATE", "EDIT", "DELETE", "APPROVE"
        public string Name { get; set; } = string.Empty; // e.g. "Xem", "Tạo mới", "Chỉnh sửa", "Xóa", "Phê duyệt"
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
