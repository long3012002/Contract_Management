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

        IQueryable<GoiThau> query = DbSet.AsNoTracking()
            .Include(gt => gt.DuAn)
            .Include(gt => gt.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau);

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
        var items = await DbSet
            .Include(gt => gt.DuAn)
            .Include(gt => gt.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .ToListAsync();
        return Mapper.Map<List<GoiThauDto>>(items);
    }

    public override async Task<GoiThauDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(gt => gt.DuAn)
            .Include(gt => gt.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .FirstOrDefaultAsync(gt => gt.Id == id);
        return entity is null ? null : Mapper.Map<GoiThauDto>(entity);
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

        // Process NhaThauGoiThaus
        if (dto.NhaThauGoiThaus != null && dto.NhaThauGoiThaus.Any())
        {
            var bidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).Distinct().ToList();
            var existingCount = await DbContext.DoiTacs.CountAsync(dt => bidderIds.Contains(dt.Id));
            if (existingCount != bidderIds.Count)
            {
                throw new KeyNotFoundException("Một hoặc nhiều nhà thầu được chọn không tồn tại.");
            }

            // Normalize for single bidder if needed
            if (dto.NhaThauGoiThaus.Count == 1)
            {
                var single = dto.NhaThauGoiThaus.First();
                single.IsLienDanh = false;
                single.TenLienDanh = null;
                single.IsDaiDienLienDanh = false;
                single.TyLeLienDanh = 100;
                single.GiaTriDamNhan = dto.GiaTriGoiThau;
            }

            // Validate
            GoiThauValidator.ValidateBidders(dto.GiaTriGoiThau, dto.NhaThauGoiThaus);

            // Add NhaThauGoiThau entities
            foreach (var inputDto in dto.NhaThauGoiThaus)
            {
                var ntgt = Mapper.Map<NhaThauGoiThau>(inputDto);
                ntgt.GoiThauId = entity.Id;
                await DbContext.NhaThauGoiThaus.AddAsync(ntgt);
            }
        }

        return entity;
    }

    public override async Task<GoiThauDto> CreateAsync(CreateGoiThauDto dto)
    {
        var entity = await CreateEntityInternalAsync(dto);
        await DbContext.SaveChangesAsync();

        // Reload to get relationship mappings
        var reloaded = await DbSet
            .Include(gt => gt.DuAn)
            .Include(gt => gt.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .FirstOrDefaultAsync(gt => gt.Id == entity.Id);
        return Mapper.Map<GoiThauDto>(reloaded);
    }

    public override async Task<IEnumerable<GoiThauDto>> CreateRangeAsync(IEnumerable<CreateGoiThauDto> dtos)
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
                .Include(gt => gt.NhaThauGoiThaus)
                    .ThenInclude(nt => nt.NhaThau)
                .FirstOrDefaultAsync(gt => gt.Id == entity.Id);
            result.Add(Mapper.Map<GoiThauDto>(reloaded));
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

        // Check if the new budget is less than the sum of contracts signed for this package
        var contractsSum = await DbContext.HopDongs
            .Where(h => h.GoiThauId == id)
            .SumAsync(h => h.GiaTriHopDong);
        if (dto.GiaTriGoiThau < contractsSum)
        {
            throw new InvalidOperationException($"Giá trị dự toán mới của gói thầu ({dto.GiaTriGoiThau:N0} VNĐ) không thể nhỏ hơn tổng giá trị hợp đồng đã ký ({contractsSum:N0} VNĐ).");
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        // Process NhaThauGoiThaus updates
        if (dto.NhaThauGoiThaus != null)
        {
            var bidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).Distinct().ToList();
            if (bidderIds.Any())
            {
                var existingCount = await DbContext.DoiTacs.CountAsync(dt => bidderIds.Contains(dt.Id));
                if (existingCount != bidderIds.Count)
                {
                    throw new KeyNotFoundException("Một hoặc nhiều nhà thầu được chọn không tồn tại.");
                }
            }

            // Normalize for single bidder if needed
            if (dto.NhaThauGoiThaus.Count == 1)
            {
                var single = dto.NhaThauGoiThaus.First();
                single.IsLienDanh = false;
                single.TenLienDanh = null;
                single.IsDaiDienLienDanh = false;
                single.TyLeLienDanh = 100;
                single.GiaTriDamNhan = dto.GiaTriGoiThau;
            }

            // Validate
            GoiThauValidator.ValidateBidders(dto.GiaTriGoiThau, dto.NhaThauGoiThaus);

            // Fetch existing
            var existingBidders = await DbContext.NhaThauGoiThaus
                .Where(nt => nt.GoiThauId == id)
                .ToListAsync();

            // 1. Remove deleted
            var incomingBidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).ToHashSet();
            var toRemove = existingBidders.Where(eb => !incomingBidderIds.Contains(eb.NhaThauId)).ToList();
            DbContext.NhaThauGoiThaus.RemoveRange(toRemove);

            // 2. Add or Update
            foreach (var inputDto in dto.NhaThauGoiThaus)
            {
                var existing = existingBidders.FirstOrDefault(eb => eb.NhaThauId == inputDto.NhaThauId);
                if (existing == null)
                {
                    var newNt = Mapper.Map<NhaThauGoiThau>(inputDto);
                    newNt.GoiThauId = id;
                    await DbContext.NhaThauGoiThaus.AddAsync(newNt);
                }
                else
                {
                    existing.IsLienDanh = inputDto.IsLienDanh;
                    existing.TenLienDanh = inputDto.TenLienDanh;
                    existing.IsDaiDienLienDanh = inputDto.IsDaiDienLienDanh;
                    existing.TyLeLienDanh = inputDto.TyLeLienDanh;
                    existing.GiaTriDamNhan = inputDto.GiaTriDamNhan;
                    existing.VaiTroTrongLienDanh = inputDto.VaiTroTrongLienDanh;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        await DbContext.SaveChangesAsync();

        return true;
    }
}
