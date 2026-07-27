using System;

namespace demo1.Entity
{
    public class ChucVu
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenChucVu { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Level { get; set; } = 999;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
