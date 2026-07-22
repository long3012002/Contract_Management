using System;

namespace demo1.Entity
{
    public class DonVi
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TenDonVi { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
