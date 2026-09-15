using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace demo1.Services.Implements
{
    public class EntityNameCacheService : IEntityNameCacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public EntityNameCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<Dictionary<string, string>> GetEntityNamesAsync(IEnumerable<Guid> guids, AppDbContext dbContext)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var uniqueGuids = guids.Where(g => g != Guid.Empty).Distinct().ToList();
            if (!uniqueGuids.Any()) return result;

            var missingGuids = new List<Guid>();

            foreach (var guid in uniqueGuids)
            {
                var cacheKey = $"EntityName_{guid}";
                if (_cache.TryGetValue<string>(cacheKey, out var name) && !string.IsNullOrEmpty(name))
                {
                    result[guid.ToString()] = name;
                }
                else
                {
                    missingGuids.Add(guid);
                }
            }

            if (!missingGuids.Any())
            {
                return result;
            }

            // Batch fetch missing GUIDs from DB
            var fetchedNames = new Dictionary<Guid, string>();

            // 1. Users
            var users = await dbContext.Users.AsNoTracking()
                .Where(u => missingGuids.Contains(u.Id))
                .Select(u => new { u.Id, Name = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Username })
                .ToListAsync();
            foreach (var item in users) fetchedNames[item.Id] = item.Name;

            // 2. DuAns
            var remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var duAns = await dbContext.DuAns.AsNoTracking()
                    .Where(d => remainingGuids.Contains(d.Id))
                    .Select(d => new { d.Id, d.Name })
                    .ToListAsync();
                foreach (var item in duAns) fetchedNames[item.Id] = item.Name;
            }

            // 3. GoiThaus
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var goiThaus = await dbContext.GoiThaus.AsNoTracking()
                    .Where(gt => remainingGuids.Contains(gt.Id))
                    .Select(gt => new { gt.Id, gt.Name })
                    .ToListAsync();
                foreach (var item in goiThaus) fetchedNames[item.Id] = item.Name;
            }

            // 4. HopDongs
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var hopDongs = await dbContext.HopDongs.AsNoTracking()
                    .Where(hd => remainingGuids.Contains(hd.Id))
                    .Select(hd => new { hd.Id, hd.Name })
                    .ToListAsync();
                foreach (var item in hopDongs) fetchedNames[item.Id] = item.Name;
            }

            // 5. DieuChinhDuAns
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var dieuChinhs = await dbContext.DieuChinhDuAns.AsNoTracking()
                    .Where(dc => remainingGuids.Contains(dc.Id))
                    .Select(dc => new { dc.Id, Name = !string.IsNullOrEmpty(dc.Name) ? dc.Name : dc.LyDoDieuChinh })
                    .ToListAsync();
                foreach (var item in dieuChinhs) fetchedNames[item.Id] = item.Name;
            }

            // 6. DoiTacs
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var doiTacs = await dbContext.DoiTacs.AsNoTracking()
                    .Where(dt => remainingGuids.Contains(dt.Id))
                    .Select(dt => new { dt.Id, dt.Name })
                    .ToListAsync();
                foreach (var item in doiTacs) fetchedNames[item.Id] = item.Name;
            }

            // 7. NhomDuAns
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var nhomDuAns = await dbContext.NhomDuAns.AsNoTracking()
                    .Where(nd => remainingGuids.Contains(nd.Id))
                    .Select(nd => new { nd.Id, nd.Name })
                    .ToListAsync();
                foreach (var item in nhomDuAns) fetchedNames[item.Id] = item.Name;
            }

            // 8. PhanLoaiDuAns
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var phanLoais = await dbContext.PhanLoaiDuAns.AsNoTracking()
                    .Where(pl => remainingGuids.Contains(pl.Id))
                    .Select(pl => new { pl.Id, pl.Name })
                    .ToListAsync();
                foreach (var item in phanLoais) fetchedNames[item.Id] = item.Name;
            }

            // 9. NguonVons
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var nguonVons = await dbContext.NguonVons.AsNoTracking()
                    .Where(nv => remainingGuids.Contains(nv.Id))
                    .Select(nv => new { nv.Id, nv.Name })
                    .ToListAsync();
                foreach (var item in nguonVons) fetchedNames[item.Id] = item.Name;
            }

            // 10. LoaiHopDongs
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var loaiHopDongs = await dbContext.LoaiHopDongs.AsNoTracking()
                    .Where(lhd => remainingGuids.Contains(lhd.Id))
                    .Select(lhd => new { lhd.Id, lhd.Name })
                    .ToListAsync();
                foreach (var item in loaiHopDongs) fetchedNames[item.Id] = item.Name;
            }

            // 11. PhongBans
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var phongBans = await dbContext.PhongBans.AsNoTracking()
                    .Where(pb => remainingGuids.Contains(pb.Id))
                    .Select(pb => new { pb.Id, Name = pb.TenPhongBan })
                    .ToListAsync();
                foreach (var item in phongBans) fetchedNames[item.Id] = item.Name;
            }

            // 12. DonVis
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var donVis = await dbContext.DonVis.AsNoTracking()
                    .Where(dv => remainingGuids.Contains(dv.Id))
                    .Select(dv => new { dv.Id, Name = dv.TenDonVi })
                    .ToListAsync();
                foreach (var item in donVis) fetchedNames[item.Id] = item.Name;
            }

            // 13. ToNhoms
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var toNhoms = await dbContext.ToNhoms.AsNoTracking()
                    .Where(tn => remainingGuids.Contains(tn.Id))
                    .Select(tn => new { tn.Id, Name = tn.TenToNhom })
                    .ToListAsync();
                foreach (var item in toNhoms) fetchedNames[item.Id] = item.Name;
            }

            // 14. ChucVus
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var chucVus = await dbContext.ChucVus.AsNoTracking()
                    .Where(cv => remainingGuids.Contains(cv.Id))
                    .Select(cv => new { cv.Id, Name = cv.TenChucVu })
                    .ToListAsync();
                foreach (var item in chucVus) fetchedNames[item.Id] = item.Name;
            }

            // 15. Licenses
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var licenses = await dbContext.Licenses.AsNoTracking()
                    .Where(lic => remainingGuids.Contains(lic.Id))
                    .Select(lic => new { lic.Id, lic.Name })
                    .ToListAsync();
                foreach (var item in licenses) fetchedNames[item.Id] = item.Name;
            }

            // 16. HangHoaDichVus
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var hangHoas = await dbContext.HangHoaDichVus.AsNoTracking()
                    .Where(hh => remainingGuids.Contains(hh.Id))
                    .Select(hh => new { hh.Id, Name = !string.IsNullOrEmpty(hh.Name) ? hh.Name : hh.TenDichVu })
                    .ToListAsync();
                foreach (var item in hangHoas) if (!string.IsNullOrEmpty(item.Name)) fetchedNames[item.Id] = item.Name;
            }

            // 17. XuatXus
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var xuatXus = await dbContext.XuatXus.AsNoTracking()
                    .Where(xx => remainingGuids.Contains(xx.Id))
                    .Select(xx => new { xx.Id, xx.Name })
                    .ToListAsync();
                foreach (var item in xuatXus) fetchedNames[item.Id] = item.Name;
            }

            // 18. DonViTinhs
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var dvts = await dbContext.DonViTinhs.AsNoTracking()
                    .Where(dvt => remainingGuids.Contains(dvt.Id))
                    .Select(dvt => new { dvt.Id, dvt.Name })
                    .ToListAsync();
                foreach (var item in dvts) fetchedNames[item.Id] = item.Name;
            }

            // 19. HangSanXuats
            remainingGuids = missingGuids.Except(fetchedNames.Keys).ToList();
            if (remainingGuids.Any())
            {
                var hsxs = await dbContext.HangSanXuats.AsNoTracking()
                    .Where(hsx => remainingGuids.Contains(hsx.Id))
                    .Select(hsx => new { hsx.Id, hsx.Name })
                    .ToListAsync();
                foreach (var item in hsxs) fetchedNames[item.Id] = item.Name;
            }

            // Store newly fetched items in Cache & result
            foreach (var kvp in fetchedNames)
            {
                var cacheKey = $"EntityName_{kvp.Key}";
                _cache.Set(cacheKey, kvp.Value, CacheDuration);
                result[kvp.Key.ToString()] = kvp.Value;
            }

            return result;
        }
    }
}
