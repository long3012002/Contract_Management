using System;

namespace demo1.DTOs
{
    public class ToNhomDto
    {
        public Guid Id { get; set; }
        public string TenToNhom { get; set; } = string.Empty;
        public Guid? IdPhongBan { get; set; }
        public string? TenPhongBan { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateToNhomDto
    {
        public string TenToNhom { get; set; } = string.Empty;
        public Guid? IdPhongBan { get; set; }
    }

    public class UpdateToNhomDto
    {
        public string TenToNhom { get; set; } = string.Empty;
        public Guid? IdPhongBan { get; set; }
    }
}
