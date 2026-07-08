using System;
using System.Collections.Generic;

namespace demo1.DTOs
{
    public class UserRolesUpdateDto
    {
        public List<Guid> RoleIds { get; set; } = new();
    }
}
