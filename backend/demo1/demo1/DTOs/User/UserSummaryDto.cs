using System;

namespace demo1.DTOs
{
    /// <summary>
    /// DTO tóm tắt thông tin cơ bản của Người dùng (phục vụ nhúng thông tin hiển thị).
    /// </summary>
    public class UserSummaryDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
