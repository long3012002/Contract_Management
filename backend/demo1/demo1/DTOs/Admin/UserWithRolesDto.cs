using System;
using System.Collections.Generic;

namespace demo1.DTOs
{
    public class UserWithRolesDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? IdPhongBan { get; set; }
        public Guid? IdChucVu { get; set; }
        public Guid? IdDonVi { get; set; }
        public string? TenPhongBan { get; set; }
        public string? TenChucVu { get; set; }
        public string? TenDonVi { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemAdmin { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
