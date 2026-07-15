using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Services.Implements;

public class GoiThauService : DbCrudService<GoiThau, GoiThauDto, CreateGoiThauDto, UpdateGoiThauDto>, IGoiThauService
{
    public GoiThauService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<PagedResult<GoiThauDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<GoiThau> query = DbSet.AsNoTracking().Include(gt => gt.DuAn);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        List<GoiThau> items;
        bool isKeyset = TryParseCursor(cursor, out var lastCreatedAt, out var lastId);

        if (isKeyset)
        {
            items = await query
                .Where(item => item.CreatedAt < lastCreatedAt || (item.CreatedAt == lastCreatedAt && item.Id.CompareTo(lastId) < 0))
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(pageSize)
                .ToListAsync();
        }
        else
        {
            items = await query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        string? nextCursor = null;
        if (items.Any())
        {
            var lastItem = items.Last();
            var hasMore = await query
                .Where(item => item.CreatedAt < lastItem.CreatedAt || (item.CreatedAt == lastItem.CreatedAt && item.Id.CompareTo(lastItem.Id) < 0))
                .AnyAsync();
            if (hasMore)
            {
                nextCursor = EncodeCursor(lastItem.CreatedAt, lastItem.Id);
            }
        }

        var dtos = Mapper.Map<List<GoiThauDto>>(items);

        return new PagedResult<GoiThauDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    public override async Task<IReadOnlyList<GoiThauDto>> GetAllItemsAsync()
    {
        var items = await DbSet.Include(gt => gt.DuAn).ToListAsync();
        return Mapper.Map<List<GoiThauDto>>(items);
    }

    public override async Task<GoiThauDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet.Include(gt => gt.DuAn).FirstOrDefaultAsync(gt => gt.Id == id);
        return entity is null ? null : Mapper.Map<GoiThauDto>(entity);
    }

    public override async Task<GoiThauDto> CreateAsync(CreateGoiThauDto dto)
    {
        GoiThauValidator.EnsureValid(dto.GiaTriGoiThau, dto.NguongCanhBaoPercent);

        if (dto.DuAnId.HasValue)
        {
            // Verify project budget limits
            var project = await DbContext.DuAns.Include(da => da.DieuChinhs)
                                             .Include(da => da.GoiThaus)
                                             .FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
            if (project == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }

            var projectBudget = project.DuToanPheDuyet + (project.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0);
            var existingPackagesSum = project.GoiThaus?.Sum(gt => gt.GiaTriGoiThau) ?? 0;

            if (existingPackagesSum + dto.GiaTriGoiThau > projectBudget)
            {
                throw new InvalidOperationException($"Tổng giá trị các gói thầu ({existingPackagesSum + dto.GiaTriGoiThau:N0} VNĐ) vượt quá tổng mức đầu tư của dự án ({projectBudget:N0} VNĐ).");
            }
        }

        var entity = Mapper.Map<GoiThau>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        // Validate unique code
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == entity.Code.ToLower());
        if (exists)
        {
            throw new InvalidOperationException($"Mã gói thầu '{entity.Code}' đã tồn tại.");
        }

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        // Reload to get relationship mappings
        var reloaded = await DbSet.Include(gt => gt.DuAn).FirstOrDefaultAsync(gt => gt.Id == entity.Id);
        return Mapper.Map<GoiThauDto>(reloaded);
    }

    public override async Task<IEnumerable<GoiThauDto>> CreateRangeAsync(IEnumerable<CreateGoiThauDto> dtos)
    {
        var result = new List<GoiThauDto>();
        foreach (var dto in dtos)
        {
            var created = await CreateAsync(dto);
            result.Add(created);
        }
        return result;
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateGoiThauDto dto)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        GoiThauValidator.EnsureValid(dto.GiaTriGoiThau, dto.NguongCanhBaoPercent);

        if (dto.DuAnId.HasValue)
        {
            var project = await DbContext.DuAns.Include(da => da.DieuChinhs)
                                             .Include(da => da.GoiThaus)
                                             .FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
            if (project == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }

            var projectBudget = project.DuToanPheDuyet + (project.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0);
            var existingPackagesSum = project.GoiThaus?.Where(gt => gt.Id != id).Sum(gt => gt.GiaTriGoiThau) ?? 0;

            if (existingPackagesSum + dto.GiaTriGoiThau > projectBudget)
            {
                throw new InvalidOperationException($"Tổng giá trị các gói thầu ({existingPackagesSum + dto.GiaTriGoiThau:N0} VNĐ) vượt quá tổng mức đầu tư của dự án ({projectBudget:N0} VNĐ).");
            }
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return true;
    }
}
