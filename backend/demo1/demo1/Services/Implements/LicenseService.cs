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
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(AppDbContext dbContext, IMapper mapper, ILogger<LicenseService> logger) : base(dbContext, mapper)
    {
        _logger = logger;
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

    public override Task<PagedResult<LicenseDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        return GetAllAsync(new LicenseFilterDto
        {
            Search = search,
            Page = page,
            PageSize = pageSize,
            Cursor = cursor
        });
    }

    public async Task<PagedResult<LicenseDto>> GetAllAsync(LicenseFilterDto filter)
    {
        try
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            IQueryable<License> query = DbSet
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Include(l => l.NhaCungCap)
                .AsNoTracking();

            if (filter.HopDongId.HasValue)
            {
                query = query.Where(l => l.HopDongId == filter.HopDongId.Value);
            }

            if (filter.DuAnId.HasValue)
            {
                query = query.Where(l => l.DuAnId == filter.DuAnId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim();
                query = ApplySearchFilter(query, keyword);
            }

            var totalItems = await query.CountAsync();
            List<License> items;
            bool isKeyset = TryParseCursor(filter.Cursor, out var lastCreatedAt, out var lastId);

            if (isKeyset)
            {
                items = await query
                    .Where(l => l.CreatedAt < lastCreatedAt || (l.CreatedAt == lastCreatedAt && l.Id.CompareTo(lastId) < 0))
                    .OrderByDescending(l => l.CreatedAt)
                    .ThenByDescending(l => l.Id)
                    .Take(pageSize)
                    .ToListAsync();
            }
            else
            {
                items = await query
                    .OrderByDescending(l => l.CreatedAt)
                    .ThenByDescending(l => l.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            string? nextCursor = null;
            if (items.Any())
            {
                var lastItem = items.Last();
                var hasMore = await query
                    .Where(l => l.CreatedAt < lastItem.CreatedAt || (l.CreatedAt == lastItem.CreatedAt && l.Id.CompareTo(lastItem.Id) < 0))
                    .AnyAsync();
                if (hasMore)
                {
                    nextCursor = EncodeCursor(lastItem.CreatedAt, lastItem.Id);
                }
            }

            var dtos = items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();

            return new PagedResult<LicenseDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                NextCursor = nextCursor
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllAsync với filter.");
            throw;
        }
    }

    public override async Task<IReadOnlyList<LicenseDto>> GetAllItemsAsync()
    {
        try
        {
            var items = await DbSet
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Include(l => l.NhaCungCap)
                .AsNoTracking()
                .ToListAsync();

            return items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllItemsAsync.");
            throw;
        }
    }

    public override async Task<LicenseDto?> GetByIdAsync(Guid id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetByIdAsync cho ID {Id}.", id);
            throw;
        }
    }

    public async Task<PagedResult<LicenseDto>> GetByDuAnIdAsync(Guid duAnId, string? search, int page, int pageSize)
    {
        try
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
                .ThenByDescending(l => l.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = items
                .Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item))
                .ToList();

            return new PagedResult<LicenseDto>
            {
                Items = dtos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetByDuAnIdAsync cho DuAnId {DuAnId}.", duAnId);
            throw;
        }
    }

    public async Task<IReadOnlyList<LicenseDto>> GetExpiringLicensesAsync(int? daysThreshold = null)
    {
        try
        {
            var today = DateTime.Today;

            var query = DbSet
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Include(l => l.NhaCungCap)
                .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
                .AsNoTracking();

            if (daysThreshold.HasValue)
            {
                var thresholdDate = today.AddDays(daysThreshold.Value);
                query = query.Where(l => l.NgayKetThuc!.Value.Date <= thresholdDate.Date);
            }
            else
            {
                query = query.Where(l => (l.NgayKetThuc!.Value - today).TotalDays <= l.CanhBaoTruocNgay);
            }

            var items = await query.OrderBy(l => l.NgayKetThuc).ToListAsync();
            return items.Select(item => EnrichDtoStatus(Mapper.Map<LicenseDto>(item), item)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetExpiringLicensesAsync.");
            throw;
        }
    }

    public async Task<LicenseSummaryDto> GetLicenseSummaryAsync(Guid? duAnId = null)
    {
        try
        {
            IQueryable<License> query = DbSet.AsNoTracking();

            if (duAnId.HasValue)
            {
                query = query.Where(l => l.DuAnId == duAnId.Value);
            }

            var today = DateTime.Today;

            var stats = await query
                .GroupBy(l => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    PerpetualCount = g.Count(l => l.LoaiLicense == 2),
                    TermBasedCount = g.Count(l => l.LoaiLicense == 1),
                    HardwareBasedCount = g.Count(l => l.LoaiLicense == 3),
                    PerUserCount = g.Count(l => l.LoaiLicense == 4),

                    TerminatedCount = g.Count(l => !l.IsActive || l.TrangThai == 4),

                    ExpiredCount = g.Count(l => l.IsActive && l.TrangThai != 4 && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue 
                        && l.NgayKetThuc!.Value.Date < today.Date),

                    ExpiringSoonCount = g.Count(l => l.IsActive && l.TrangThai != 4 && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue 
                        && (l.NgayKetThuc!.Value - today).TotalDays >= 0 
                        && (l.NgayKetThuc!.Value - today).TotalDays <= l.CanhBaoTruocNgay)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                return new LicenseSummaryDto();
            }

            return new LicenseSummaryDto
            {
                TotalCount = stats.TotalCount,
                PerpetualCount = stats.PerpetualCount,
                TermBasedCount = stats.TermBasedCount,
                HardwareBasedCount = stats.HardwareBasedCount,
                PerUserCount = stats.PerUserCount,
                ActiveCount = stats.TotalCount - stats.TerminatedCount - stats.ExpiredCount - stats.ExpiringSoonCount,
                ExpiringSoonCount = stats.ExpiringSoonCount,
                ExpiredCount = stats.ExpiredCount,
                TerminatedCount = stats.TerminatedCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetLicenseSummaryAsync.");
            throw;
        }
    }

    public override async Task<LicenseDto> CreateAsync(CreateLicenseDto dto)
    {
        try
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

            if (entity.CanhBaoTruocNgay <= 0)
            {
                entity.CanhBaoTruocNgay = 30;
            }

            if (string.IsNullOrWhiteSpace(entity.Code))

            {
                entity.Code = $"LIC-{DateTime.Now:yyyyMMdd}-{entity.Id.ToString().Substring(0, 4).ToUpper()}";
            }

            CalculateEndAndDuration(entity);
            entity.TrangThai = RecalculateStatus(entity);

            await DbSet.AddAsync(entity);
            await DbContext.SaveChangesAsync();

            return (await GetByIdAsync(entity.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CreateAsync của LicenseService.");
            throw;
        }
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateLicenseDto dto)
    {
        try
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
            CalculateEndAndDuration(entity);
            entity.TrangThai = RecalculateStatus(entity);

            await DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong UpdateAsync của LicenseService cho ID {Id}.", id);
            throw;
        }
    }

    private static void CalculateEndAndDuration(License license)
    {
        if (license.LoaiLicense == 2)
        {
            license.NgayKetThuc = null;
            license.ThoiHan = null;
            return;
        }

        if (license.NgayBatDau.HasValue && !string.IsNullOrWhiteSpace(license.ThoiHan) && !license.NgayKetThuc.HasValue)
        {
            license.NgayKetThuc = CalculateEndDate(license.NgayBatDau.Value, license.ThoiHan);
        }
        else if (license.NgayBatDau.HasValue && license.NgayKetThuc.HasValue && string.IsNullOrWhiteSpace(license.ThoiHan))
        {
            var days = (license.NgayKetThuc.Value.Date - license.NgayBatDau.Value.Date).Days;
            if (days >= 30 && days % 30 == 0)
            {
                var months = days / 30;
                license.ThoiHan = $"{months} tháng";
            }
            else
            {
                license.ThoiHan = $"{days} ngày";
            }
        }
    }

    private static DateTime? CalculateEndDate(DateTime startDate, string durationStr)
    {
        durationStr = durationStr.Trim().ToLower();

        if (int.TryParse(durationStr, out var num))
        {
            return startDate.AddMonths(num);
        }

        var match = System.Text.RegularExpressions.Regex.Match(durationStr, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var val))
        {
            if (durationStr.Contains("ngày") || durationStr.Contains("day"))
            {
                return startDate.AddDays(val);
            }
            if (durationStr.Contains("tháng") || durationStr.Contains("month"))
            {
                return startDate.AddMonths(val);
            }
            if (durationStr.Contains("năm") || durationStr.Contains("year"))
            {
                return startDate.AddYears(val);
            }

            return startDate.AddMonths(val);
        }

        return null;
    }

    public async Task<SyncContractLicensesResultDto> SyncContractLicensesAsync(Guid hopDongId, SyncContractLicensesDto dto)
    {
        var hopDong = await DbContext.HopDongs.FirstOrDefaultAsync(h => h.Id == hopDongId);
        if (hopDong == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Hợp đồng với ID '{hopDongId}'.");
        }

        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. Fetch existing active licenses associated with this contract
            var existingLicenses = await DbSet
                .Where(l => l.HopDongId == hopDongId && l.IsActive)
                .ToListAsync();

            var incomingItems = dto.Items ?? new List<SyncLicenseItemDto>();
            var incomingIds = incomingItems
                .Where(i => i.Id.HasValue && i.Id.Value != Guid.Empty)
                .Select(i => i.Id!.Value)
                .ToHashSet();

            int createdCount = 0;
            int updatedCount = 0;
            int deletedCount = 0;

            // 2. Mark missing items as soft-deleted (IsActive = false)
            foreach (var existing in existingLicenses)
            {
                if (!incomingIds.Contains(existing.Id))
                {
                    existing.IsActive = false;
                    existing.UpdatedAt = DateTime.UtcNow;
                    deletedCount++;
                }
            }

            // 3. Update existing or Create new items
            foreach (var item in incomingItems)
            {
                if (item.Id.HasValue && item.Id.Value != Guid.Empty)
                {
                    var existing = existingLicenses.FirstOrDefault(l => l.Id == item.Id.Value);
                    if (existing != null)
                    {
                        existing.Code = item.Code;
                        existing.Name = item.Name;
                        existing.Description = item.Description;
                        existing.DuAnId = item.DuAnId != Guid.Empty ? item.DuAnId : (hopDong.DuAnId ?? Guid.Empty);
                        existing.HopDongId = hopDongId;
                        existing.NhaCungCapId = item.NhaCungCapId ?? hopDong.NhaThauId;
                        existing.LoaiLicense = item.LoaiLicense;
                        existing.SoLuong = item.SoLuong;
                        existing.ThongTinThietBi = item.ThongTinThietBi;
                        existing.NgayBatDau = item.NgayBatDau;
                        existing.ThoiHan = item.ThoiHan;
                        existing.NgayKetThuc = item.NgayKetThuc;
                        existing.CanhBaoTruocNgay = item.CanhBaoTruocNgay;
                        existing.TrangThai = item.TrangThai;
                        existing.IsActive = item.IsActive;
                        existing.GhiChu = item.GhiChu;
                        existing.UpdatedAt = DateTime.UtcNow;

                        CalculateEndAndDuration(existing);
                        existing.TrangThai = RecalculateStatus(existing);
                        updatedCount++;
                    }
                }
                else
                {
                    var newLicense = new License
                    {
                        Id = Guid.NewGuid(),
                        Code = string.IsNullOrWhiteSpace(item.Code) ? $"LIC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4]}" : item.Code,
                        Name = item.Name,
                        Description = item.Description,
                        DuAnId = item.DuAnId != Guid.Empty ? item.DuAnId : (hopDong.DuAnId ?? Guid.Empty),

                        HopDongId = hopDongId,
                        NhaCungCapId = item.NhaCungCapId ?? hopDong.NhaThauId,
                        LoaiLicense = item.LoaiLicense,
                        SoLuong = item.SoLuong,
                        ThongTinThietBi = item.ThongTinThietBi,
                        NgayBatDau = item.NgayBatDau,
                        ThoiHan = item.ThoiHan,
                        NgayKetThuc = item.NgayKetThuc,
                        CanhBaoTruocNgay = item.CanhBaoTruocNgay > 0 ? item.CanhBaoTruocNgay : 30,
                        TrangThai = item.TrangThai,
                        IsActive = true,
                        GhiChu = item.GhiChu,
                        CreatedAt = DateTime.UtcNow
                    };

                    CalculateEndAndDuration(newLicense);
                    newLicense.TrangThai = RecalculateStatus(newLicense);

                    await DbSet.AddAsync(newLicense);
                    createdCount++;
                }
            }

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Fetch refreshed list and return
            var updatedLicenses = await DbSet
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Include(l => l.NhaCungCap)
                .Where(l => l.HopDongId == hopDongId && l.IsActive)
                .AsNoTracking()
                .ToListAsync();

            var resultDtos = updatedLicenses
                .Select(l => EnrichDtoStatus(Mapper.Map<LicenseDto>(l), l))
                .ToList();

            return new SyncContractLicensesResultDto
            {
                HopDongId = hopDongId,
                CreatedCount = createdCount,
                UpdatedCount = updatedCount,
                DeletedCount = deletedCount,
                Items = resultDtos
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi khi sync danh sách License cho Hợp đồng {HopDongId}", hopDongId);
            throw;
        }
    }
}

