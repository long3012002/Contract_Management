namespace demo1.DTOs
{
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? TenPhongBan { get; set; }
        public string? TenChucVu { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemAdmin { get; set; } = false;
    }
}
