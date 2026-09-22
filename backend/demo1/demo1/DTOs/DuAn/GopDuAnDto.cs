using System;

namespace demo1.DTOs;

public class GopDuAnDto
{
    /// <summary>
    /// Mã GUID của Dự án đích (Dự án nhận gộp)
    /// </summary>
    public Guid TargetDuAnId { get; set; }

    /// <summary>
    /// Lý do hoặc ghi chú thực hiện gộp dự án
    /// </summary>
    public string? GhiChu { get; set; }
}

public class DuAnGopLinkDto
{
    public Guid Id { get; set; }
    public Guid SourceDuAnId { get; set; }
    public string SourceMaDuAn { get; set; } = string.Empty;
    public string SourceTenDuAn { get; set; } = string.Empty;

    public Guid TargetDuAnId { get; set; }
    public string TargetMaDuAn { get; set; } = string.Empty;
    public string TargetTenDuAn { get; set; } = string.Empty;

    public DateTime NgayGop { get; set; }
    public Guid NguoiThucHienId { get; set; }
    public string NguoiThucHienName { get; set; } = string.Empty;

    public decimal DuToanLucGop { get; set; }
    public string? GhiChu { get; set; }
}

public class HuyGopDuAnDto
{
    /// <summary>
    /// ID của bản ghi liên kết gộp DuAnGopLink (nếu truyền trực tiếp)
    /// </summary>
    public Guid? GopLinkId { get; set; }

    /// <summary>
    /// Mã GUID của Dự án nguồn đã bị gộp cần hủy gộp (nếu không truyền GopLinkId)
    /// </summary>
    public Guid? SourceDuAnId { get; set; }

    /// <summary>
    /// Lý do hoặc ghi chú thực hiện hủy gộp dự án
    /// </summary>
    public string? GhiChu { get; set; }
}
