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
        public bool IsActive { get; set; }
        public bool IsSystemAdmin { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
