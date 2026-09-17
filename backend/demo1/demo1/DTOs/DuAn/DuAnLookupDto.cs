using System;

namespace demo1.DTOs;

/// <summary>
/// DTO thông tin rút gọn của Dự án phục vụ hiển thị Dropdown / Lookup
/// </summary>
public class DuAnLookupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
