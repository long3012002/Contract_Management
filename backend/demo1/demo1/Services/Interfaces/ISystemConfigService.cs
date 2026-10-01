using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace demo1.Services.Interfaces;

/// <summary>
/// Service quản lý cấu hình hệ thống lưu trong database.
/// Ưu tiên đọc từ DB, fallback về appsettings nếu không có.
/// Cache kết quả 5 phút để tránh query DB liên tục trong workers.
/// </summary>
public interface ISystemConfigService
{
    /// <summary>Lấy tất cả config, nhóm theo Group.</summary>
    Task<List<demo1.DTOs.SystemConfig.SystemConfigDto>> GetAllAsync();

    /// <summary>Lấy giá trị string của một key. Fallback về defaultValue nếu không có.</summary>
    Task<string> GetStringAsync(string key, string defaultValue = "");

    /// <summary>Lấy giá trị int của một key. Fallback về defaultValue nếu không có.</summary>
    Task<int> GetIntAsync(string key, int defaultValue = 0);

    /// <summary>Lấy giá trị bool của một key. Fallback về defaultValue nếu không có.</summary>
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);

    /// <summary>Cập nhật nhiều config cùng lúc (batch). Tự invalidate cache sau khi lưu.</summary>
    Task<int> UpdateBatchAsync(List<demo1.DTOs.SystemConfig.UpdateSystemConfigItemDto> items, string updatedByUsername);

    /// <summary>Reset một key về giá trị mặc định.</summary>
    Task ResetToDefaultAsync(string key, string updatedByUsername);

    /// <summary>Invalidate cache thủ công (dùng khi cần reload ngay lập tức).</summary>
    void InvalidateCache();
}
