using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs.SystemConfig;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace demo1.Services.Implements;

/// <summary>
/// Triển khai ISystemConfigService.
/// Logic ưu tiên: DB → appsettings fallback.
/// Toàn bộ config được cache trong MemoryCache với TTL 5 phút.
/// </summary>
public class SystemConfigService : ISystemConfigService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SystemConfigService> _logger;

    // Cache key tập trung để invalidate đồng nhất
    private const string CacheKey = "system_config_all";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public SystemConfigService(
        AppDbContext db,
        IConfiguration config,
        IMemoryCache cache,
        ILogger<SystemConfigService> logger)
    {
        _db = db;
        _config = config;
        _cache = cache;
        _logger = logger;
    }

    // ─── Đọc toàn bộ config (có cache) ──────────────────────────────────────

    public async Task<List<SystemConfigDto>> GetAllAsync()
    {
        var records = await GetCachedRecordsAsync();
        return records.Select(MapToDto).OrderBy(x => x.Group).ThenBy(x => x.SortOrder).ToList();
    }

    // ─── Typed getters ────────────────────────────────────────────────────────

    public async Task<string> GetStringAsync(string key, string defaultValue = "")
    {
        var records = await GetCachedRecordsAsync();
        var record = records.FirstOrDefault(r => r.Key == key);
        if (record != null) return record.Value;

        // Fallback: đọc từ appsettings (chuyển dot-path)
        var cfgKey = key.Replace(":", ":");
        var cfgValue = _config[cfgKey];
        return cfgValue ?? defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var strVal = await GetStringAsync(key, defaultValue.ToString());
        return int.TryParse(strVal, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var strVal = await GetStringAsync(key, defaultValue.ToString());
        return bool.TryParse(strVal, out var result) ? result : defaultValue;
    }

    // ─── Writes ──────────────────────────────────────────────────────────────

    public async Task<int> UpdateBatchAsync(
        List<UpdateSystemConfigItemDto> items,
        string updatedByUsername)
    {
        if (items == null || items.Count == 0) return 0;

        var keys = items.Select(i => i.Key).ToList();
        var existing = await _db.SystemConfigs
            .Where(c => keys.Contains(c.Key))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var updatedCount = 0;

        foreach (var item in items)
        {
            var record = existing.FirstOrDefault(e => e.Key == item.Key);
            if (record == null)
            {
                _logger.LogWarning("[SystemConfig] Key '{Key}' không tồn tại trong DB, bỏ qua.", item.Key);
                continue;
            }

            if (!ValidateValue(record.DataType, item.Value))
            {
                _logger.LogWarning("[SystemConfig] Giá trị '{Value}' không hợp lệ cho key '{Key}' (kiểu {DataType}).", item.Value, item.Key, record.DataType);
                continue;
            }

            record.Value = item.Value.Trim();
            record.UpdatedAt = now;
            record.UpdatedByUsername = updatedByUsername;
            updatedCount++;
        }

        await _db.SaveChangesAsync();
        InvalidateCache();

        _logger.LogInformation("[SystemConfig] User '{User}' cập nhật {Count} config keys.", updatedByUsername, updatedCount);
        return updatedCount;
    }

    public async Task ResetToDefaultAsync(string key, string updatedByUsername)
    {
        var record = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
        if (record == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy config key '{key}'.");
        }

        record.Value = record.DefaultValue;
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedByUsername = updatedByUsername;
        await _db.SaveChangesAsync();
        InvalidateCache();

        _logger.LogInformation("[SystemConfig] Key '{Key}' được reset về mặc định bởi '{User}'.", key, updatedByUsername);
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<List<SystemConfig>> GetCachedRecordsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out List<SystemConfig>? cached) && cached != null)
            return cached;

        var records = await _db.SystemConfigs.AsNoTracking().ToListAsync();

        _cache.Set(CacheKey, records, CacheTtl);
        return records;
    }

    private static SystemConfigDto MapToDto(SystemConfig c) => new()
    {
        Key = c.Key,
        Value = c.Value,
        DataType = c.DataType,
        Group = c.Group,
        Label = c.Label,
        Description = c.Description,
        DefaultValue = c.DefaultValue,
        SortOrder = c.SortOrder,
        UpdatedAt = c.UpdatedAt,
        UpdatedByUsername = c.UpdatedByUsername
    };

    private static bool ValidateValue(string dataType, string value) => dataType switch
    {
        "int" => int.TryParse(value, out _),
        "bool" => bool.TryParse(value, out _),
        _ => true   // string: luôn hợp lệ
    };
}
