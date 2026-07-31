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
using Microsoft.Extensions.Logging;
using demo1.Data;

namespace demo1.Services.Implements;

public class GoiThauService : DbCrudService<GoiThau, GoiThauDto, CreateGoiThauDto, UpdateGoiThauDto>, IGoiThauService
{
    private readonly ILogger<GoiThauService> _logger;

    public GoiThauService(AppDbContext dbContext, IMapper mapper, ILogger<GoiThauService> logger) : base(dbContext, mapper)
    {
        _logger = logger;
    }

    public override Task<PagedResult<GoiThauDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        return GetAllAsync(new GoiThauFilterDto
        {
            Search = search,
            Page = page,
            PageSize = pageSize,
            Cursor = cursor
        });
    }

    public async Task<PagedResult<GoiThauDto>> GetAllAsync(GoiThauFilterDto filter)
    {
        try
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            IQueryable<GoiThau> query = DbSet.AsNoTracking()
                .Include(gt => gt.DuAn);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim();
                query = ApplySearchFilter(query, keyword);
            }

            if (filter.DuAnId.HasValue)
            {
                query = query.Where(item => item.DuAnId == filter.DuAnId.Value);
            }

            if (filter.MinGiaTri.HasValue)
            {
                query = query.Where(item => item.GiaTriGoiThau >= filter.MinGiaTri.Value);
            }

            if (filter.MaxGiaTri.HasValue)
            {
                query = query.Where(item => item.GiaTriGoiThau <= filter.MaxGiaTri.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(item => item.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(item => item.CreatedAt <= filter.ToDate.Value);
            }

            var totalItems = await query.CountAsync();

            List<GoiThau> items;
            bool isKeyset = TryParseCursor(filter.Cursor, out var lastCreatedAt, out var lastId);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<IReadOnlyList<GoiThauDto>> GetAllItemsAsync()
    {
        try
        {
            var items = await DbSet
                .Include(gt => gt.DuAn)
                .ToListAsync();
            return Mapper.Map<List<GoiThauDto>>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllItemsAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<GoiThauDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await DbSet
                .Include(gt => gt.DuAn)
                .FirstOrDefaultAsync(gt => gt.Id == id);
            return entity is null ? null : Mapper.Map<GoiThauDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetByIdAsync của GoiThauService cho ID {Id}.", id);
            throw;
        }
    }

    private async Task<GoiThau> CreateEntityInternalAsync(
        CreateGoiThauDto dto, 
        HashSet<string>? existingCodesInBatch = null,
        Dictionary<Guid, decimal>? projectBatchSum = null)
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

            decimal batchSumForProject = 0;
            if (projectBatchSum != null && projectBatchSum.TryGetValue(dto.DuAnId.Value, out var sum))
            {
                batchSumForProject = sum;
            }

            if (existingPackagesSum + batchSumForProject + dto.GiaTriGoiThau > projectBudget)
            {
                throw new InvalidOperationException($"Tổng giá trị các gói thầu ({existingPackagesSum + batchSumForProject + dto.GiaTriGoiThau:N0} VNĐ) vượt quá tổng mức đầu tư của dự án ({projectBudget:N0} VNĐ).");
            }

            if (projectBatchSum != null)
            {
                projectBatchSum[dto.DuAnId.Value] = batchSumForProject + dto.GiaTriGoiThau;
            }
        }

        var entity = Mapper.Map<GoiThau>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        var codeLower = entity.Code.ToLower();
        // Validate unique code in DB
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == codeLower);
        if (exists)
        {
            throw new InvalidOperationException($"Mã gói thầu '{entity.Code}' đã tồn tại.");
        }

        // Validate unique code in batch if provided
        if (existingCodesInBatch != null)
        {
            if (existingCodesInBatch.Contains(codeLower))
            {
                throw new InvalidOperationException($"Mã gói thầu '{entity.Code}' bị trùng lặp trong danh sách thêm mới.");
            }
            existingCodesInBatch.Add(codeLower);
        }

        await DbSet.AddAsync(entity);

        return entity;
    }

    public override async Task<GoiThauDto> CreateAsync(CreateGoiThauDto dto)
    {
        try
        {
            var entity = await CreateEntityInternalAsync(dto);
            await DbContext.SaveChangesAsync();

            // Reload to get relationship mappings
            var reloaded = await DbSet
                .Include(gt => gt.DuAn)
                .FirstOrDefaultAsync(gt => gt.Id == entity.Id);
            return Mapper.Map<GoiThauDto>(reloaded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CreateAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<IEnumerable<GoiThauDto>> CreateRangeAsync(IEnumerable<CreateGoiThauDto> dtos)
    {
        try
        {
            var entities = new List<GoiThau>();
            var codesInBatch = new HashSet<string>();
            var projectBatchSum = new Dictionary<Guid, decimal>();

            foreach (var dto in dtos)
            {
                var entity = await CreateEntityInternalAsync(dto, codesInBatch, projectBatchSum);
                entities.Add(entity);
            }

            await DbContext.SaveChangesAsync();

            var result = new List<GoiThauDto>();
            foreach (var entity in entities)
            {
                var reloaded = await DbSet
                    .Include(gt => gt.DuAn)
                    .FirstOrDefaultAsync(gt => gt.Id == entity.Id);
                result.Add(Mapper.Map<GoiThauDto>(reloaded));
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CreateRangeAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateGoiThauDto dto)
    {
        try
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

            var contractsSum = await DbContext.HopDongs
                .Where(h => h.GoiThauId == id)
                .SumAsync(h => h.GiaTriHopDong);
            if (dto.GiaTriGoiThau < contractsSum)
            {
                throw new InvalidOperationException($"Giá trị dự toán mới của gói thầu ({dto.GiaTriGoiThau:N0} VNĐ) không thể nhỏ hơn tổng giá trị hợp đồng đã ký ({contractsSum:N0} VNĐ).");
            }

            Mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong UpdateAsync của GoiThauService cho ID {Id}.", id);
            throw;
        }
    }
}
