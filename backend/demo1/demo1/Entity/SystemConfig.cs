using System;

namespace demo1.Entity;

/// <summary>
/// Lưu các cấu hình hệ thống do Admin quản lý qua UI.
/// Thay thế việc phải sửa appsettings.json và restart server.
/// </summary>
public class SystemConfig
{
    /// <summary>Khóa duy nhất, dạng "Group:Key" (VD: "Email:WarnDaysContract").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Giá trị lưu dưới dạng string (parse theo DataType khi dùng).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Kiểu dữ liệu: "int" | "bool" | "string".</summary>
    public string DataType { get; set; } = "string";

    /// <summary>Nhóm để hiển thị trên UI: "EMAIL" | "NOTIFICATION" | "AUDIT" | "BUSINESS".</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Nhãn hiển thị trên UI.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Mô tả ngắn hỗ trợ admin hiểu ý nghĩa.</summary>
    public string? Description { get; set; }

    /// <summary>Giá trị mặc định để reset.</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>Thứ tự hiển thị trong nhóm.</summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>Thời điểm cập nhật gần nhất (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người cập nhật gần nhất.</summary>
    public string? UpdatedByUsername { get; set; }
}
