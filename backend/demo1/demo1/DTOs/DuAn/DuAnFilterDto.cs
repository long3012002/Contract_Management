using System;

namespace demo1.DTOs;

public class DuAnFilterDto
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Cursor { get; set; }

    public int? LoaiDuAn { get; set; }
}
