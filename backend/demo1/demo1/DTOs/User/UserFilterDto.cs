using System;

namespace demo1.DTOs;

public class UserFilterDto
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public bool? IsActive { get; set; }
    public bool? IsSystemAdmin { get; set; }
    public Guid? IdPhongBan { get; set; }
    public Guid? IdChucVu { get; set; }
    public Guid? IdDonVi { get; set; }
    public string? Role { get; set; }
}
