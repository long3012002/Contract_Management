using System;

namespace demo1.DTOs
{
    /// <summary>
    /// Bộ lọc danh sách License phần mềm.
    /// </summary>
    public class LicenseFilterDto
    {
        public string? Search { get; set; }
        public Guid? HopDongId { get; set; }
        public Guid? DuAnId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Cursor { get; set; }
    }
}
