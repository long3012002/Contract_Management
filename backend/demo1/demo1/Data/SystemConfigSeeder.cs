using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace demo1.Data;

/// <summary>
/// Seeder cho bảng SystemConfig — chạy khi SeedMasterData = true.
/// Dùng pattern Upsert: nếu key chưa có thì thêm, nếu đã có thì bỏ qua (không overwrite giá trị admin đã sửa).
/// </summary>
public static class SystemConfigSeeder
{
    public static async Task SeedAsync(AppDbContext context, ILogger? logger = null)
    {
        logger?.LogInformation("[SystemConfigSeeder] Bắt đầu seed cấu hình hệ thống mặc định...");

        var defaults = GetDefaultConfigs();
        var existingKeys = await context.SystemConfigs.Select(c => c.Key).ToListAsync();

        var toAdd = defaults.Where(d => !existingKeys.Contains(d.Key)).ToList();

        if (toAdd.Count == 0)
        {
            logger?.LogInformation("[SystemConfigSeeder] Tất cả config đã tồn tại, bỏ qua.");
            return;
        }

        await context.SystemConfigs.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        logger?.LogInformation("[SystemConfigSeeder] Đã thêm {Count} config mặc định.", toAdd.Count);
    }

    private static List<SystemConfig> GetDefaultConfigs() =>
    [
        // ── Nhóm EMAIL ──────────────────────────────────────────────────────
        new SystemConfig
        {
            Key          = "Email:Enabled",
            Value        = "true",
            DefaultValue = "true",
            DataType     = "bool",
            Group        = "EMAIL",
            Label        = "Kích hoạt email thông báo",
            Description  = "Bật/tắt toàn bộ tính năng gửi email tự động của hệ thống.",
            SortOrder    = 1,
            UpdatedAt    = DateTime.UtcNow
        },
        new SystemConfig
        {
            Key          = "Email:WarnDaysContract",
            Value        = "30",
            DefaultValue = "30",
            DataType     = "int",
            Group        = "EMAIL",
            Label        = "Ngưỡng cảnh báo hợp đồng (ngày)",
            Description  = "Gửi email khi hợp đồng còn ít hơn hoặc bằng số ngày này trước ngày hết hạn.",
            SortOrder    = 2,
            UpdatedAt    = DateTime.UtcNow
        },
        new SystemConfig
        {
            Key          = "Email:WarnDaysLicense",
            Value        = "30",
            DefaultValue = "30",
            DataType     = "int",
            Group        = "EMAIL",
            Label        = "Ngưỡng cảnh báo license (ngày)",
            Description  = "Ngưỡng mặc định cho license không tự cấu hình riêng.",
            SortOrder    = 3,
            UpdatedAt    = DateTime.UtcNow
        },
        new SystemConfig
        {
            Key          = "Email:WarnDaysPayment",
            Value        = "30",
            DefaultValue = "30",
            DataType     = "int",
            Group        = "EMAIL",
            Label        = "Ngưỡng cảnh báo đợt thanh toán (ngày)",
            Description  = "Gửi email khi đợt thanh toán còn ít hơn hoặc bằng số ngày này.",
            SortOrder    = 4,
            UpdatedAt    = DateTime.UtcNow
        },
        new SystemConfig
        {
            Key          = "Email:SenderName",
            Value        = "Quản lý Hợp đồng",
            DefaultValue = "Quản lý Hợp đồng",
            DataType     = "string",
            Group        = "EMAIL",
            Label        = "Tên người gửi email",
            Description  = "Tên hiển thị trong trường \"From\" của email gửi đi.",
            SortOrder    = 5,
            UpdatedAt    = DateTime.UtcNow
        },

        // ── Nhóm NOTIFICATION ───────────────────────────────────────────────
        new SystemConfig
        {
            Key          = "Notification:IntervalDays",
            Value        = "1",
            DefaultValue = "1",
            DataType     = "int",
            Group        = "NOTIFICATION",
            Label        = "Khoảng cách nhắc lại thông báo (ngày)",
            Description  = "Số ngày giữa hai lần nhắc cùng một thông báo in-app. Đặt 0 để chỉ nhắc một lần.",
            SortOrder    = 1,
            UpdatedAt    = DateTime.UtcNow
        },

        // ── Nhóm AUDIT ──────────────────────────────────────────────────────
        new SystemConfig
        {
            Key          = "Audit:RetentionDays",
            Value        = "90",
            DefaultValue = "90",
            DataType     = "int",
            Group        = "AUDIT",
            Label        = "Số ngày lưu trữ audit log",
            Description  = "Audit log cũ hơn số ngày này sẽ bị xóa tự động. Tối thiểu 7 ngày.",
            SortOrder    = 1,
            UpdatedAt    = DateTime.UtcNow
        },

        // ── Nhóm BUSINESS ───────────────────────────────────────────────────
        new SystemConfig
        {
            Key          = "Business:LicenseDefaultWarnDays",
            Value        = "30",
            DefaultValue = "30",
            DataType     = "int",
            Group        = "BUSINESS",
            Label        = "Số ngày cảnh báo license mặc định",
            Description  = "Giá trị mặc định điền sẵn vào trường \"Cảnh báo trước\" khi tạo License mới.",
            SortOrder    = 1,
            UpdatedAt    = DateTime.UtcNow
        }
    ];
}
