using System;

namespace demo1.DTOs;

public class LicenseWarningDto
{
    public Guid LicenseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DuAnName { get; set; }
    public string? HopDongName { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public int DaysRemaining { get; set; }
    public int CanhBaoTruocNgay { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
}
