using System.Collections.Generic;

namespace demo1.DTOs;

/// <summary>
/// DTO chứa thông tin chi tiết gói thầu và danh sách công việc liên quan.
/// </summary>
public class GoiThauDetailWithTasksDto
{
    public GoiThauDto Detail { get; set; } = null!;
    public List<CongViecGoiThauDto> CongViecs { get; set; } = new List<CongViecGoiThauDto>();
}
