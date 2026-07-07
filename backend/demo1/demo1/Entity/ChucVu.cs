using System;

namespace demo1.Entity
{
    public class ChucVu
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenChucVu { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
