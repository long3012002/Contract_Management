using System;

namespace demo1.Entity
{
    public class PhongBan
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenPhongBan { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
