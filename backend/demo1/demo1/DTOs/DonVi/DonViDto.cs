using System;

namespace demo1.DTOs
{
    public class DonViDto
    {
        public Guid Id { get; set; }
        public string TenDonVi { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDonViDto
    {
        public string TenDonVi { get; set; } = string.Empty;
    }

    public class UpdateDonViDto
    {
        public string TenDonVi { get; set; } = string.Empty;
    }
}
