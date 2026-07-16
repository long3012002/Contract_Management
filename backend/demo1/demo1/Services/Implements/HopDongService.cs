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

public class HopDongService : DbCrudService<HopDong, HopDongDto, CreateHopDongDto, UpdateHopDongDto>, IHopDongService
{
    public HopDongService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<PagedResult<HopDongDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<HopDong> query = DbSet.AsNoTracking()
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item => 
                EF.Functions.Like(item.Code, $"%{keyword}%") || 
                EF.Functions.Like(item.Name, $"%{keyword}%") ||
                (item.Description != null && EF.Functions.Like(item.Description, $"%{keyword}%")));
        }

        var totalItems = await query.CountAsync();

        List<HopDong> items;
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

        var dtos = Mapper.Map<List<HopDongDto>>(items);

        return new PagedResult<HopDongDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    public override async Task<IReadOnlyList<HopDongDto>> GetAllItemsAsync()
    {
        var items = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .ToListAsync();
        return Mapper.Map<List<HopDongDto>>(items);
    }

    public override async Task<HopDongDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .FirstOrDefaultAsync(h => h.Id == id);
        return entity is null ? null : Mapper.Map<HopDongDto>(entity);
    }

    public override async Task<HopDongDto> CreateAsync(CreateHopDongDto dto)
    {
        HopDongValidator.EnsureValid(dto.GiaTriHopDong, dto.DotThanhToans);

        // Check DuAn existence
        if (dto.DuAnId.HasValue)
        {
            var duAnExists = await DbContext.DuAns.AnyAsync(da => da.Id == dto.DuAnId.Value);
            if (!duAnExists)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }
        }

        // Check GoiThau uniqueness for contracts
        if (dto.GoiThauId.HasValue)
        {
            var goiThau = await DbContext.GoiThaus.FirstOrDefaultAsync(gt => gt.Id == dto.GoiThauId.Value);
            if (goiThau == null)
            {
                throw new KeyNotFoundException("Không tìm thấy gói thầu được liên kết.");
            }

            var alreadyLinked = await DbSet.AnyAsync(h => h.GoiThauId == dto.GoiThauId.Value);
            if (alreadyLinked)
            {
                throw new InvalidOperationException("Gói thầu này đã được liên kết với một hợp đồng khác.");
            }

            if (dto.GiaTriHopDong > goiThau.GiaTriGoiThau)
            {
                throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được lớn hơn giá trị dự toán của gói thầu ({goiThau.GiaTriGoiThau:N0} VNĐ).");
            }
        }

        // Check ChuDauTu and NhaThau existence
        if (dto.ChuDauTuId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.ChuDauTuId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin chủ đầu tư.");
        }
        if (dto.NhaThauId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaThauId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin nhà thầu.");
        }

        var entity = Mapper.Map<HopDong>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        // Ensure unique code
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == entity.Code.ToLower());
        if (exists)
        {
            throw new InvalidOperationException($"Số ký hiệu hợp đồng '{entity.Code}' đã tồn tại.");
        }

        // Add payment installments
        if (dto.DotThanhToans != null)
        {
            foreach (var dotDto in dto.DotThanhToans)
            {
                var dot = Mapper.Map<DotThanhToan>(dotDto);
                dot.Id = Guid.NewGuid();
                dot.HopDongId = entity.Id;
                // Calculate payment value based on percentage if not explicitly set or to ensure consistency
                dot.GiaTriThanhToan = dot.TyLeThanhToan * entity.GiaTriHopDong / 100;
                dot.NgayThanhToan = dotDto.NgayThanhToan;
                dot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                dot.CreatedAt = DateTime.UtcNow;
                entity.DotThanhToans.Add(dot);
            }
        }

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        var reloaded = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .FirstOrDefaultAsync(h => h.Id == entity.Id);

        return Mapper.Map<HopDongDto>(reloaded);
    }

    public override async Task<IEnumerable<HopDongDto>> CreateRangeAsync(IEnumerable<CreateHopDongDto> dtos)
    {
        var result = new List<HopDongDto>();
        foreach (var dto in dtos)
        {
            var created = await CreateAsync(dto);
            result.Add(created);
        }
        return result;
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateHopDongDto dto)
    {
        var entity = await DbSet.Include(h => h.DotThanhToans).FirstOrDefaultAsync(h => h.Id == id);
        if (entity is null)
        {
            return false;
        }

        HopDongValidator.EnsureValid(dto.GiaTriHopDong, dto.DotThanhToans);

        // Ensure unique code
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == dto.Code.ToLower() && item.Id != id);
        if (exists)
        {
            throw new InvalidOperationException($"Số ký hiệu hợp đồng '{dto.Code}' đã tồn tại.");
        }

        // Check DuAn existence
        if (dto.DuAnId.HasValue)
        {
            var duAnExists = await DbContext.DuAns.AnyAsync(da => da.Id == dto.DuAnId.Value);
            if (!duAnExists)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }
        }

        // Check GoiThau uniqueness for contracts
        if (dto.GoiThauId.HasValue)
        {
            var goiThau = await DbContext.GoiThaus.FirstOrDefaultAsync(gt => gt.Id == dto.GoiThauId.Value);
            if (goiThau == null)
            {
                throw new KeyNotFoundException("Không tìm thấy gói thầu được liên kết.");
            }

            var alreadyLinked = await DbSet.AnyAsync(h => h.GoiThauId == dto.GoiThauId.Value && h.Id != id);
            if (alreadyLinked)
            {
                throw new InvalidOperationException("Gói thầu này đã được liên kết với một hợp đồng khác.");
            }

            if (dto.GiaTriHopDong > goiThau.GiaTriGoiThau)
            {
                throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được lớn hơn giá trị dự toán của gói thầu ({goiThau.GiaTriGoiThau:N0} VNĐ).");
            }
        }

        // Check ChuDauTu and NhaThau existence
        if (dto.ChuDauTuId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.ChuDauTuId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin chủ đầu tư.");
        }
        if (dto.NhaThauId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaThauId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin nhà thầu.");
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        // Clear existing payment installments and add new ones
        if (entity.DotThanhToans.Any())
        {
            foreach (var oldDot in entity.DotThanhToans.ToList())
            {
                DbContext.Entry(oldDot).State = EntityState.Deleted;
            }
        }

        if (dto.DotThanhToans != null)
        {
            foreach (var dotDto in dto.DotThanhToans)
            {
                var dot = Mapper.Map<DotThanhToan>(dotDto);
                dot.Id = Guid.NewGuid();
                dot.HopDongId = entity.Id;
                dot.GiaTriThanhToan = dot.TyLeThanhToan * entity.GiaTriHopDong / 100;
                dot.NgayThanhToan = dotDto.NgayThanhToan;
                dot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                dot.CreatedAt = DateTime.UtcNow;
                entity.DotThanhToans.Add(dot);
            }
        }

        await DbContext.SaveChangesAsync();
        return true;
    }
}
