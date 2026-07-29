using System;

namespace demo1.DTOs;

public class GoiThauFilterDto
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Cursor { get; set; }

    public Guid? DuAnId { get; set; }
    public decimal? MinGiaTri { get; set; }
    public decimal? MaxGiaTri { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
