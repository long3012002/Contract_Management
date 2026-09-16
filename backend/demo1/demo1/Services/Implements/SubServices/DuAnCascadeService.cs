using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Services.Interfaces.SubServices;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements.SubServices;

public class DuAnCascadeService : IDuAnCascadeService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDuAnNguonLinkService _nguonLinkService;

    public DuAnCascadeService(
        AppDbContext dbContext,
        ICurrentUserService currentUserService,
        IDuAnNguonLinkService nguonLinkService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _nguonLinkService = nguonLinkService;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _dbContext.DuAns.FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            return false;
        }

        // 1. Tìm và xoá tất cả hợp đồng liên quan tới dự án hoặc gói thầu thuộc dự án
        var hopDongs = await _dbContext.HopDongs
            .Where(hd => hd.DuAnId == id || (hd.GoiThau != null && hd.GoiThau.DuAnId == id))
            .ToListAsync();
        if (hopDongs.Any())
        {
            _dbContext.HopDongs.RemoveRange(hopDongs);
        }

        // 2. Tìm và xoá tất cả gói thầu thuộc dự án
        var goiThaus = await _dbContext.GoiThaus
            .Where(gt => gt.DuAnId == id)
            .ToListAsync();
        if (goiThaus.Any())
        {
            _dbContext.GoiThaus.RemoveRange(goiThaus);
        }

        if (entity.LoaiDuAn == 2)
        {
            var link = await _dbContext.DuAnNguonTrienKhais
                .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == id);
            var sourceIds = link?.NguonProjectId?
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(Guid.Parse)
                .ToList() ?? new List<Guid>();
            if (sourceIds.Any())
            {
                var sourceProjects = await _dbContext.DuAns.Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                foreach (var sp in sourceProjects)
                {
                    sp.DaTrienKhai = false;
                }
            }
        }

        _dbContext.DuAns.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public Task<bool> SoftDeleteAsync(Guid id)
    {
        return SoftDeleteAsync(new[] { id });
    }

    public async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await _dbContext.DuAns.Where(da => idList.Contains(da.Id) && !da.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var userId = _currentUserService.GetUserId();
        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedByUserId = userId;
            entity.UpdatedAt = now;
        }

        // Cascade Soft Delete cho Gói thầu
        var goiThaus = await _dbContext.GoiThaus
            .Where(gt => gt.DuAnId.HasValue && idList.Contains(gt.DuAnId.Value))
            .ToListAsync();
        var goiThauIds = goiThaus.Select(gt => gt.Id).ToList();

        foreach (var gt in goiThaus)
        {
            gt.IsDeleted = true;
            gt.DeletedAt = now;
            gt.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Hợp đồng
        var hopDongs = await _dbContext.HopDongs
            .Where(hd => (hd.DuAnId.HasValue && idList.Contains(hd.DuAnId.Value)) || (hd.GoiThauId.HasValue && goiThauIds.Contains(hd.GoiThauId.Value)))
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();

        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = true;
            hd.DeletedAt = now;
            hd.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Công việc gói thầu
        var congViecs = await _dbContext.CongViecGoiThaus
            .Where(cv => cv.GoiThau != null && cv.GoiThau.DuAnId.HasValue && idList.Contains(cv.GoiThau.DuAnId.Value))
            .ToListAsync();
        var congViecIds = congViecs.Select(cv => cv.Id).ToList();

        foreach (var cv in congViecs)
        {
            cv.IsDeleted = true;
            cv.DeletedAt = now;
            cv.DeletedByUserId = userId;
        }

        if (congViecIds.Any())
        {
            var comments = await _dbContext.CommentCongViecGoiThaus
                .Where(c => congViecIds.Contains(c.CongViecGoiThauId))
                .ToListAsync();
            foreach (var c in comments)
            {
                c.IsDeleted = true;
                c.DeletedAt = now;
                c.DeletedByUserId = userId;
            }

            var nlqs = await _dbContext.CongViecNguoiLienQuans
                .Where(nlq => congViecIds.Contains(nlq.CongViecGoiThauId))
                .ToListAsync();
            foreach (var nlq in nlqs)
            {
                nlq.IsDeleted = true;
                nlq.DeletedAt = now;
                nlq.DeletedByUserId = userId;
            }

            var lss = await _dbContext.CongViecLichSuChuyenTieps
                .Where(ls => congViecIds.Contains(ls.CongViecGoiThauId))
                .ToListAsync();
            foreach (var ls in lss)
            {
                ls.IsDeleted = true;
                ls.DeletedAt = now;
                ls.DeletedByUserId = userId;
            }
        }

        // Cascade Soft Delete cho Điều chỉnh dự án
        var dieuChinhs = await _dbContext.DieuChinhDuAns
            .Where(dc => idList.Contains(dc.DuAnId))
            .ToListAsync();
        foreach (var dc in dieuChinhs)
        {
            dc.IsDeleted = true;
            dc.DeletedAt = now;
            dc.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho License / Bản quyền
        var licenses = await _dbContext.Licenses
            .Where(l => idList.Contains(l.DuAnId) || (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value)))
            .ToListAsync();
        foreach (var l in licenses)
        {
            l.IsDeleted = true;
            l.DeletedAt = now;
            l.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Hàng hóa dịch vụ thuộc Hợp đồng
        if (hopDongIds.Any())
        {
            var hangHoas = await _dbContext.HangHoaDichVus
                .Where(h => hopDongIds.Contains(h.IdParent))
                .ToListAsync();
            foreach (var h in hangHoas)
            {
                h.IsDeleted = true;
                h.DeletedAt = now;
                h.DeletedByUserId = userId;
            }
        }

        // Hủy trạng thái đã triển khai dự án nguồn nếu có
        foreach (var entity in entities)
        {
            if (entity.LoaiDuAn == 2)
            {
                var link = await _dbContext.DuAnNguonTrienKhais
                    .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == entity.Id);
                var sourceIds = link?.NguonProjectId?
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();
                if (sourceIds.Any())
                {
                    var sourceProjects = await _dbContext.DuAns.Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                    foreach (var sp in sourceProjects)
                    {
                        sp.DaTrienKhai = false;
                    }
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public Task<bool> RestoreAsync(Guid id)
    {
        return RestoreAsync(new[] { id });
    }

    public async Task<bool> RestoreAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await _dbContext.DuAns.IgnoreQueryFilters().Where(e => idList.Contains(e.Id) && e.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;
            entity.UpdatedAt = now;
        }

        // Khôi phục tất cả gói thầu liên quan bị xóa mềm
        var goiThaus = await _dbContext.GoiThaus.IgnoreQueryFilters()
            .Where(gt => gt.DuAnId.HasValue && idList.Contains(gt.DuAnId.Value) && gt.IsDeleted)
            .ToListAsync();
        var goiThauIds = goiThaus.Select(gt => gt.Id).ToList();
        foreach (var gt in goiThaus)
        {
            gt.IsDeleted = false;
            gt.DeletedAt = null;
            gt.DeletedByUserId = null;
        }

        // Khôi phục tất cả hợp đồng liên quan bị xóa mềm
        var hopDongs = await _dbContext.HopDongs.IgnoreQueryFilters()
            .Where(hd => ((hd.DuAnId.HasValue && idList.Contains(hd.DuAnId.Value)) || (hd.GoiThauId.HasValue && goiThauIds.Contains(hd.GoiThauId.Value))) && hd.IsDeleted)
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();
        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = false;
            hd.DeletedAt = null;
            hd.DeletedByUserId = null;
        }

        // Khôi phục tất cả công việc liên quan bị xóa mềm
        var congViecs = await _dbContext.CongViecGoiThaus.IgnoreQueryFilters()
            .Where(cv => cv.GoiThau != null && cv.GoiThau.DuAnId.HasValue && idList.Contains(cv.GoiThau.DuAnId.Value) && cv.IsDeleted)
            .ToListAsync();
        var congViecIds = congViecs.Select(cv => cv.Id).ToList();
        foreach (var cv in congViecs)
        {
            cv.IsDeleted = false;
            cv.DeletedAt = null;
            cv.DeletedByUserId = null;
        }

        if (congViecIds.Any())
        {
            var comments = await _dbContext.CommentCongViecGoiThaus.IgnoreQueryFilters()
                .Where(c => congViecIds.Contains(c.CongViecGoiThauId) && c.IsDeleted)
                .ToListAsync();
            foreach (var c in comments)
            {
                c.IsDeleted = false;
                c.DeletedAt = null;
                c.DeletedByUserId = null;
            }

            var nlqs = await _dbContext.CongViecNguoiLienQuans.IgnoreQueryFilters()
                .Where(nlq => congViecIds.Contains(nlq.CongViecGoiThauId) && nlq.IsDeleted)
                .ToListAsync();
            foreach (var nlq in nlqs)
            {
                nlq.IsDeleted = false;
                nlq.DeletedAt = null;
                nlq.DeletedByUserId = null;
            }

            var lss = await _dbContext.CongViecLichSuChuyenTieps.IgnoreQueryFilters()
                .Where(ls => congViecIds.Contains(ls.CongViecGoiThauId) && ls.IsDeleted)
                .ToListAsync();
            foreach (var ls in lss)
            {
                ls.IsDeleted = false;
                ls.DeletedAt = null;
                ls.DeletedByUserId = null;
            }
        }

        var dieuChinhs = await _dbContext.DieuChinhDuAns.IgnoreQueryFilters()
            .Where(dc => idList.Contains(dc.DuAnId) && dc.IsDeleted)
            .ToListAsync();
        foreach (var dc in dieuChinhs)
        {
            dc.IsDeleted = false;
            dc.DeletedAt = null;
            dc.DeletedByUserId = null;
        }

        var licenses = await _dbContext.Licenses.IgnoreQueryFilters()
            .Where(l => (idList.Contains(l.DuAnId) || (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value))) && l.IsDeleted)
            .ToListAsync();
        foreach (var l in licenses)
        {
            l.IsDeleted = false;
            l.DeletedAt = null;
            l.DeletedByUserId = null;
        }

        if (hopDongIds.Any())
        {
            var hangHoas = await _dbContext.HangHoaDichVus.IgnoreQueryFilters()
                .Where(h => hopDongIds.Contains(h.IdParent) && h.IsDeleted)
                .ToListAsync();
            foreach (var h in hangHoas)
            {
                h.IsDeleted = false;
                h.DeletedAt = null;
                h.DeletedByUserId = null;
            }
        }

        // Đánh dấu lại trạng thái đã triển khai dự án nguồn nếu cần
        foreach (var entity in entities)
        {
            if (entity.LoaiDuAn == 2)
            {
                var link = await _dbContext.DuAnNguonTrienKhais
                    .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == entity.Id);
                var sourceIds = link?.NguonProjectId?
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();
                if (sourceIds.Any())
                {
                    var otherLinkedSourceIds = await _nguonLinkService.GetLinkedSourceProjectIdsAsync(excludeTrienKhaiProjectId: entity.Id);
                    var conflictedId = sourceIds.FirstOrDefault(id => otherLinkedSourceIds.Contains(id));
                    if (conflictedId != Guid.Empty)
                    {
                        var conflictedProj = await _dbContext.DuAns.IgnoreQueryFilters().FirstOrDefaultAsync(da => da.Id == conflictedId);
                        var projName = conflictedProj?.Name ?? conflictedId.ToString();
                        throw new InvalidOperationException($"Không thể khôi phục dự án '{entity.Name}'. Dự án nguồn '{projName}' đã thuộc về một dự án triển khai khác.");
                    }

                    var sourceProjects = await _dbContext.DuAns.IgnoreQueryFilters().Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                    foreach (var sp in sourceProjects)
                    {
                        sp.DaTrienKhai = true;
                    }
                }
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }
}
