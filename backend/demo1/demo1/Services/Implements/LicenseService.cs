using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class LicenseService : DbCrudService<License, LicenseDto, CreateLicenseDto, UpdateLicenseDto>, ILicenseService
{
    public LicenseService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    private static int RecalculateStatus(License license)
    {
        if (!license.IsActive || license.TrangThai == 4)
        {
            return 4; // Terminated / Inactive
        }

        if (license.LoaiLicense == 2 || !license.NgayKetThuc.HasValue)
        {
            return 1; // Perpetual / Active
        }

        var daysRemaining = (license.NgayKetThuc.Value.Date - DateTime.Today).Days;
        if (daysRemaining < 0)
        {
            return 3; // Expired
        }
        if (daysRemaining <= license.CanhBaoTruocNgay)
        {
            return 2; // Expiring Soon
        }

        return 1; // Active
    }

    private static LicenseDto EnrichDtoStatus(LicenseDto dto, License license)
    {
        dto.TrangThai = RecalculateStatus(license);
        return dto;
    }

    protected override IQueryable<License> ApplySearchFilter(IQueryable<License> query, string keyword)
    {
        return query.Where(l =>
            EF.Functions.Like(l.Code, $"%{keyword}%") ||
            EF.Functions.Like(l.Name, $"%{keyword}%") ||
            (l.ThongTinThietBi != null && EF.Functions.Like(l.ThongTinThietBi, $"%{keyword}%")) ||
            (l.Description != null && EF.Functions.Like(l.Description, $"%{keyword}%")) ||
            (l.DuAn != null && EF.Functions.Like(l.DuAn.Name, $"%{keyword}%")));
    }

    public override async Task<PagedResult<LicenseDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<License> query = DbSet
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();

        return new PagedResult<LicenseDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public override async Task<IReadOnlyList<LicenseDto>> GetAllItemsAsync()
    {
        var items = await DbSet
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .AsNoTracking()
            .ToListAsync();

        return items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();
    }

    public override async Task<LicenseDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (entity is null) return null;

        var dto = Mapper.Map<LicenseDto>(entity);
        return EnrichDtoStatus(dto, entity);
    }

    public async Task<PagedResult<LicenseDto>> GetByDuAnIdAsync(Guid duAnId, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<License> query = DbSet
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .Where(l => l.DuAnId == duAnId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();

        return new PagedResult<LicenseDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<IReadOnlyList<LicenseDto>> GetExpiringLicensesAsync(int? daysThreshold = null)
    {
        var today = DateTime.Today;

        var items = await DbSet
            .Include(l => l.DuAn)
            .Include(l => l.HopDong)
            .Include(l => l.NhaCungCap)
            .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
            .AsNoTracking()
            .ToListAsync();

        var expiring = items
            .Where(l =>
            {
                var daysRemaining = (l.NgayKetThuc!.Value.Date - today).Days;
                var threshold = daysThreshold ?? l.CanhBaoTruocNgay;
                return daysRemaining <= threshold;
            })
            .OrderBy(l => l.NgayKetThuc)
            .ToList();

        return expiring.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();
    }

    public async Task<LicenseSummaryDto> GetLicenseSummaryAsync(Guid? duAnId = null)
    {
        IQueryable<License> query = DbSet.AsNoTracking();

        if (duAnId.HasValue)
        {
            query = query.Where(l => l.DuAnId == duAnId.Value);
        }

        var items = await query.ToListAsync();

        var summary = new LicenseSummaryDto
        {
            TotalCount = items.Count,
            PerpetualCount = items.Count(l => l.LoaiLicense == 2),
            TermBasedCount = items.Count(l => l.LoaiLicense == 1),
            HardwareBasedCount = items.Count(l => l.LoaiLicense == 3),
            PerUserCount = items.Count(l => l.LoaiLicense == 4)
        };

        foreach (var item in items)
        {
            var status = RecalculateStatus(item);
            switch (status)
            {
                case 1: summary.ActiveCount++; break;
                case 2: summary.ExpiringSoonCount++; break;
                case 3: summary.ExpiredCount++; break;
                case 4: summary.TerminatedCount++; break;
            }
        }

        return summary;
    }

    public override async Task<LicenseDto> CreateAsync(CreateLicenseDto dto)
    {
        var duAnExists = await DbContext.DuAns.AnyAsync(d => d.Id == dto.DuAnId);
        if (!duAnExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án với ID '{dto.DuAnId}'.");
        }

        if (dto.HopDongId.HasValue)
        {
            var hopDongExists = await DbContext.HopDongs.AnyAsync(h => h.Id == dto.HopDongId.Value);
            if (!hopDongExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy Hợp đồng với ID '{dto.HopDongId.Value}'.");
            }
        }

        if (dto.NhaCungCapId.HasValue)
        {
            var doiTacExists = await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaCungCapId.Value);
            if (!doiTacExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy Đối tác với ID '{dto.NhaCungCapId.Value}'.");
            }
        }

        var entity = Mapper.Map<License>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(entity.Code))
        {
            entity.Code = $"LIC-{DateTime.Now:yyyyMMdd}-{entity.Id.ToString().Substring(0, 4).ToUpper()}";
        }

        entity.TrangThai = RecalculateStatus(entity);

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateLicenseDto dto)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        var duAnExists = await DbContext.DuAns.AnyAsync(d => d.Id == dto.DuAnId);
        if (!duAnExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án với ID '{dto.DuAnId}'.");
        }

        if (dto.HopDongId.HasValue)
        {
            var hopDongExists = await DbContext.HopDongs.AnyAsync(h => h.Id == dto.HopDongId.Value);
            if (!hopDongExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy Hợp đồng với ID '{dto.HopDongId.Value}'.");
            }
        }

        if (dto.NhaCungCapId.HasValue)
        {
            var doiTacExists = await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaCungCapId.Value);
            if (!doiTacExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy Đối tác với ID '{dto.NhaCungCapId.Value}'.");
            }
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.TrangThai = RecalculateStatus(entity);

        await DbContext.SaveChangesAsync();
        return true;
    }
}
