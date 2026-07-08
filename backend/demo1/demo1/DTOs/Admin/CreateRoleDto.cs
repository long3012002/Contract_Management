using System;

namespace demo1.DTOs
{
    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool? IsInherit { get; set; }
        public Guid? InheritRoleId { get; set; }
    }
}
