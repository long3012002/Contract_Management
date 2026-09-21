using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Services.Interfaces.SubServices;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements.SubServices;

public class DuAnAuditService : IDuAnAuditService
{
    private readonly AppDbContext _dbContext;
    private readonly IDuAnSecurityService _securityService;
    private readonly IEntityNameCacheService? _entityNameCacheService;

    private static readonly Regex GuidRegex = new Regex(
        @"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}",
        RegexOptions.Compiled);

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public DuAnAuditService(
        AppDbContext dbContext,
        IDuAnSecurityService securityService,
        IEntityNameCacheService? entityNameCacheService = null)
    {
        _dbContext = dbContext;
        _securityService = securityService;
        _entityNameCacheService = entityNameCacheService;
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id)
    {
        var entity = await _dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity != null)
        {
            await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");
        }

        var projectIdStr = id.ToString();
        var projectIdStrLower = projectIdStr.ToLower();

        var duAnTableNames = new[] { "duans", "duan", "dự án" };
        var dieuChinhTableNames = new[] { "kehoachvons", "kehoachvon", "kế hoạch vốn" };
        var goiThauTableNames = new[] { "goithaus", "goithau", "gói thầu" };
        var hopDongTableNames = new[] { "hopdongs", "hopdong", "hợp đồng" };

        var dieuChinhIds = await _dbContext.KeHoachVonDuAns
            .Where(dc => dc.DuAnId == id)
            .Select(dc => dc.KeHoachVonId.ToString())
            .ToListAsync();

        var goiThauIds = await _dbContext.GoiThaus
            .Where(gt => gt.DuAnId == id)
            .Select(gt => gt.Id.ToString())
            .ToListAsync();

        var hopDongIds = await _dbContext.HopDongs
            .Include(hd => hd.GoiThau)
            .Where(hd => hd.GoiThau != null && hd.GoiThau.DuAnId == id)
            .Select(hd => hd.Id.ToString())
            .ToListAsync();

        var dieuChinhIdsLower = dieuChinhIds.Select(i => i.ToLower()).ToList();
        var goiThauIdsLower = goiThauIds.Select(i => i.ToLower()).ToList();
        var hopDongIdsLower = hopDongIds.Select(i => i.ToLower()).ToList();

        var logs = await _dbContext.AuditLogs
            .Where(log => log.EntityId != null && (
                (duAnTableNames.Contains(log.TableName.ToLower()) && log.EntityId.ToLower() == projectIdStrLower) ||
                (dieuChinhTableNames.Contains(log.TableName.ToLower()) && dieuChinhIdsLower.Contains(log.EntityId.ToLower())) ||
                (goiThauTableNames.Contains(log.TableName.ToLower()) && goiThauIdsLower.Contains(log.EntityId.ToLower())) ||
                (hopDongTableNames.Contains(log.TableName.ToLower()) && hopDongIdsLower.Contains(log.EntityId.ToLower()))
            ))
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        if (logs.Any())
        {
            var guidStrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var log in logs)
            {
                GatherGuidsFromJson(log.OldValues, guidStrings);
                GatherGuidsFromJson(log.NewValues, guidStrings);
                if (!string.IsNullOrEmpty(log.EntityId))
                {
                    guidStrings.Add(log.EntityId);
                }
            }

            var guidList = guidStrings
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

            var entityNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (guidList.Any())
            {
                if (_entityNameCacheService != null)
                {
                    entityNameMap = await _entityNameCacheService.GetEntityNamesAsync(guidList, _dbContext);
                }
                else
                {
                    // Fallback to direct DB batch queries if cache service is not injected
                    var dbUsers = await _dbContext.Users
                        .Where(u => guidList.Contains(u.Id))
                        .Select(u => new { u.Id, Name = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Username })
                        .ToListAsync();
                    foreach (var u in dbUsers)
                    {
                        entityNameMap[u.Id.ToString()] = u.Name;
                    }

                    var dbDuAns = await _dbContext.DuAns
                        .Where(d => guidList.Contains(d.Id))
                        .Select(d => new { d.Id, Name = d.Name })
                        .ToListAsync();
                    foreach (var d in dbDuAns)
                    {
                        entityNameMap[d.Id.ToString()] = d.Name;
                    }

                    var dbGoiThaus = await _dbContext.GoiThaus
                        .Where(gt => guidList.Contains(gt.Id))
                        .Select(gt => new { gt.Id, Name = gt.Name })
                        .ToListAsync();
                    foreach (var gt in dbGoiThaus)
                    {
                        entityNameMap[gt.Id.ToString()] = gt.Name;
                    }

                    var dbHopDongs = await _dbContext.HopDongs
                        .Where(hd => guidList.Contains(hd.Id))
                        .Select(hd => new { hd.Id, Name = hd.Name })
                        .ToListAsync();
                    foreach (var hd in dbHopDongs)
                    {
                        entityNameMap[hd.Id.ToString()] = hd.Name;
                    }

                    var dbKeHoachVons = await _dbContext.KeHoachVons
                        .Where(dc => guidList.Contains(dc.Id))
                        .Select(dc => new { dc.Id, Name = dc.SoQuyetDinh })
                        .ToListAsync();
                    foreach (var dc in dbKeHoachVons)
                    {
                        entityNameMap[dc.Id.ToString()] = !string.IsNullOrEmpty(dc.Name) ? dc.Name : "Kế hoạch vốn";
                    }

                    var dbDoiTacs = await _dbContext.DoiTacs
                        .Where(dt => guidList.Contains(dt.Id))
                        .Select(dt => new { dt.Id, Name = dt.Name })
                        .ToListAsync();
                    foreach (var dt in dbDoiTacs)
                    {
                        entityNameMap[dt.Id.ToString()] = dt.Name;
                    }

                    var dbNhomDuAns = await _dbContext.NhomDuAns
                        .Where(nd => guidList.Contains(nd.Id))
                        .Select(nd => new { nd.Id, Name = nd.Name })
                        .ToListAsync();
                    foreach (var item in dbNhomDuAns)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbPhanLoai = await _dbContext.PhanLoaiDuAns
                        .Where(pl => guidList.Contains(pl.Id))
                        .Select(pl => new { pl.Id, Name = pl.Name })
                        .ToListAsync();
                    foreach (var item in dbPhanLoai)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbNguonVon = await _dbContext.NguonVons
                        .Where(nv => guidList.Contains(nv.Id))
                        .Select(nv => new { nv.Id, Name = nv.Name })
                        .ToListAsync();
                    foreach (var item in dbNguonVon)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbLoaiHopDong = await _dbContext.LoaiHopDongs
                        .Where(lhd => guidList.Contains(lhd.Id))
                        .Select(lhd => new { lhd.Id, Name = lhd.Name })
                        .ToListAsync();
                    foreach (var item in dbLoaiHopDong)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbPhongBan = await _dbContext.PhongBans
                        .Where(pb => guidList.Contains(pb.Id))
                        .Select(pb => new { pb.Id, Name = pb.TenPhongBan })
                        .ToListAsync();
                    foreach (var item in dbPhongBan)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbDonVi = await _dbContext.DonVis
                        .Where(dv => guidList.Contains(dv.Id))
                        .Select(dv => new { dv.Id, Name = dv.TenDonVi })
                        .ToListAsync();
                    foreach (var item in dbDonVi)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbToNhom = await _dbContext.ToNhoms
                        .Where(tn => guidList.Contains(tn.Id))
                        .Select(tn => new { tn.Id, Name = tn.TenToNhom })
                        .ToListAsync();
                    foreach (var item in dbToNhom)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbChucVu = await _dbContext.ChucVus
                        .Where(cv => guidList.Contains(cv.Id))
                        .Select(cv => new { cv.Id, Name = cv.TenChucVu })
                        .ToListAsync();
                    foreach (var item in dbChucVu)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbLicenses = await _dbContext.Licenses
                        .Where(lic => guidList.Contains(lic.Id))
                        .Select(lic => new { lic.Id, Name = lic.Name })
                        .ToListAsync();
                    foreach (var item in dbLicenses)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbHangHoa = await _dbContext.HangHoaDichVus
                        .Where(hh => guidList.Contains(hh.Id))
                        .Select(hh => new { hh.Id, Name = !string.IsNullOrEmpty(hh.Name) ? hh.Name : hh.TenDichVu })
                        .ToListAsync();
                    foreach (var item in dbHangHoa)
                    {
                        if (!string.IsNullOrEmpty(item.Name)) entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbXuatXu = await _dbContext.XuatXus
                        .Where(xx => guidList.Contains(xx.Id))
                        .Select(xx => new { xx.Id, Name = xx.Name })
                        .ToListAsync();
                    foreach (var item in dbXuatXu)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbDonViTinh = await _dbContext.DonViTinhs
                        .Where(dvt => guidList.Contains(dvt.Id))
                        .Select(dvt => new { dvt.Id, Name = dvt.Name })
                        .ToListAsync();
                    foreach (var item in dbDonViTinh)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }

                    var dbHangSanXuat = await _dbContext.HangSanXuats
                        .Where(hsx => guidList.Contains(hsx.Id))
                        .Select(hsx => new { hsx.Id, Name = hsx.Name })
                        .ToListAsync();
                    foreach (var item in dbHangSanXuat)
                    {
                        entityNameMap[item.Id.ToString()] = item.Name;
                    }
                }
            }

            foreach (var log in logs)
            {
                var tableLower = log.TableName?.ToLower() ?? string.Empty;
                if (tableLower == "duans" || tableLower == "duan") log.TableName = "Dự án";
                else if (tableLower == "dieuchinhduans" || tableLower == "dieuchinhduan") log.TableName = "Điều chỉnh dự án";
                else if (tableLower == "goithaus" || tableLower == "goithau") log.TableName = "Gói thầu";
                else if (tableLower == "hopdongs" || tableLower == "hopdong") log.TableName = "Hợp đồng";

                var actionUpper = log.Action?.ToUpper() ?? string.Empty;

                if (actionUpper == "CREATE" || actionUpper == "TẠO MỚI")
                {
                    log.Action = "CREATE";
                    log.OldValues = null;
                    log.NewValues = ProcessAndFormatJson(log.NewValues, entityNameMap);
                }
                else if (actionUpper == "DELETE" || actionUpper == "XÓA")
                {
                    log.Action = "DELETE";
                    log.OldValues = ProcessAndFormatJson(log.OldValues, entityNameMap);
                    log.NewValues = null;
                }
                else if (actionUpper == "UPDATE" || actionUpper == "CẬP NHẬT")
                {
                    log.Action = "UPDATE";
                    ProcessUpdateLogValues(log, entityNameMap);
                }
                else
                {
                    log.OldValues = ProcessAndFormatJson(log.OldValues, entityNameMap);
                    log.NewValues = ProcessAndFormatJson(log.NewValues, entityNameMap);
                }
            }
        }

        return logs;
    }

    private void GatherGuidsFromJson(string? json, HashSet<string> guidStrings)
    {
        if (string.IsNullOrEmpty(json)) return;
        var matches = GuidRegex.Matches(json);
        foreach (Match match in matches)
        {
            guidStrings.Add(match.Value);
        }
    }

    private string? ReplaceGuidsInJson(string? json, Dictionary<string, string> userMap)
    {
        if (string.IsNullOrEmpty(json)) return json;
        return GuidRegex.Replace(json, match =>
        {
            if (userMap.TryGetValue(match.Value, out var name))
            {
                return name;
            }
            return match.Value;
        });
    }

    private Dictionary<string, object?>? ParseJsonToDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.Clone();
            }
            return dict;
        }
        catch
        {
            return null;
        }
    }

    private string ReplaceGuidsInValue(object? val, Dictionary<string, string> entityNameMap)
    {
        if (val == null) return string.Empty;

        if (val is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }
            var rawStr = element.ToString();
            return ReplaceGuidsInJson(rawStr, entityNameMap) ?? string.Empty;
        }

        var str = val.ToString() ?? string.Empty;
        return ReplaceGuidsInJson(str, entityNameMap) ?? string.Empty;
    }

    private string? ProcessAndFormatJson(string? json, Dictionary<string, string> entityNameMap)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        var dict = ParseJsonToDictionary(json);
        if (dict == null)
        {
            return ReplaceGuidsInJson(json, entityNameMap);
        }

        var formatted = new Dictionary<string, object?>();
        foreach (var kvp in dict)
        {
            var translatedKey = AppDbContext.TranslateColumnName(kvp.Key);
            var valStr = ReplaceGuidsInValue(kvp.Value, entityNameMap);
            formatted[translatedKey] = valStr;
        }

        return JsonSerializer.Serialize(formatted, AuditJsonOptions);
    }

    private void ProcessUpdateLogValues(AuditLog log, Dictionary<string, string> entityNameMap)
    {
        var oldDict = ParseJsonToDictionary(log.OldValues);
        var newDict = ParseJsonToDictionary(log.NewValues);

        if (oldDict == null && newDict == null)
        {
            return;
        }

        var translatedOld = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var translatedNew = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (oldDict != null)
        {
            foreach (var kvp in oldDict)
            {
                var translatedKey = AppDbContext.TranslateColumnName(kvp.Key);
                translatedOld[translatedKey] = kvp.Value;
            }
        }

        if (newDict != null)
        {
            foreach (var kvp in newDict)
            {
                var translatedKey = AppDbContext.TranslateColumnName(kvp.Key);
                translatedNew[translatedKey] = kvp.Value;
            }
        }

        var allKeys = translatedOld.Keys.Union(translatedNew.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var filteredOld = new Dictionary<string, object?>();
        var filteredNew = new Dictionary<string, object?>();
        var changedColumns = new List<string>();

        foreach (var key in allKeys)
        {
            translatedOld.TryGetValue(key, out var rawOld);
            translatedNew.TryGetValue(key, out var rawNew);

            var oldStr = ReplaceGuidsInValue(rawOld, entityNameMap);
            var newStr = ReplaceGuidsInValue(rawNew, entityNameMap);

            var normOld = string.IsNullOrWhiteSpace(oldStr) ? string.Empty : oldStr.Trim();
            var normNew = string.IsNullOrWhiteSpace(newStr) ? string.Empty : newStr.Trim();

            if (!string.Equals(normOld, normNew, StringComparison.Ordinal))
            {
                changedColumns.Add(key);
                if (rawOld != null) filteredOld[key] = oldStr;
                if (rawNew != null) filteredNew[key] = newStr;
            }
        }

        log.ChangedColumns = changedColumns.Any() ? JsonSerializer.Serialize(changedColumns, AuditJsonOptions) : null;
        log.OldValues = filteredOld.Any() ? JsonSerializer.Serialize(filteredOld, AuditJsonOptions) : null;
        log.NewValues = filteredNew.Any() ? JsonSerializer.Serialize(filteredNew, AuditJsonOptions) : null;
    }
}
