using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs.HangHoaDichVu;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class HangHoaDichVuService : DbCrudService<HangHoaDichVu, HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>, IHangHoaDichVuService
{
    public HangHoaDichVuService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    protected override IQueryable<HangHoaDichVu> GetQueryable()
    {
        return DbSet.AsNoTracking()
            .Include(h => h.XuatXu)
            .Include(h => h.HangSanXuat)
            .Include(h => h.License)
            .Include(h => h.DonViTinh);
    }

    public async Task<PagedResult<HangHoaDichVuDto>> GetAllAsync(string? search, int page, int pageSize, LoaiHangHoaDichVu? loai, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<HangHoaDichVu> query = GetQueryable();

        if (loai.HasValue)
        {
            query = query.Where(h => h.Loai == loai.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        bool isKeyset = TryParseCursor(cursor, out var lastCreatedAt, out var lastId);
        var totalItems = await query.CountAsync();
        List<HangHoaDichVu> items;

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

        var dtos = Mapper.Map<List<HangHoaDichVuDto>>(items);

        return new PagedResult<HangHoaDichVuDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    protected override IQueryable<HangHoaDichVu> ApplySearchFilter(IQueryable<HangHoaDichVu> query, string keyword)
    {
        return query.Where(item =>
            (item.DanhMucHangHoa != null && EF.Functions.Like(item.DanhMucHangHoa, $"%{keyword}%")) ||
            (item.TenDichVu != null && EF.Functions.Like(item.TenDichVu, $"%{keyword}%")) ||
            (item.KyMaHieu != null && EF.Functions.Like(item.KyMaHieu, $"%{keyword}%")) ||
            (item.CauHinhTinhNangKyThuatCoBan != null && EF.Functions.Like(item.CauHinhTinhNangKyThuatCoBan, $"%{keyword}%")) ||
            (item.MoTaDichVu != null && EF.Functions.Like(item.MoTaDichVu, $"%{keyword}%")));
    }

    public async Task<IEnumerable<HangHoaDichVuDto>> GetByIdParentAsync(Guid idParent, LoaiHangHoaDichVu? loai = null)
    {
        var query = GetQueryable()
            .Where(h => h.IdParent == idParent);

        if (loai.HasValue)
        {
            query = query.Where(h => h.Loai == loai.Value);
        }

        var entities = await query.ToListAsync();
        return Mapper.Map<IEnumerable<HangHoaDichVuDto>>(entities);
    }

    public override async Task<HangHoaDichVuDto> CreateAsync(CreateHangHoaDichVuDto dto)
    {
        await ValidateParentExistsAsync(new[] { dto.IdParent });
        return await base.CreateAsync(dto);
    }

    public override async Task<IEnumerable<HangHoaDichVuDto>> CreateRangeAsync(IEnumerable<CreateHangHoaDichVuDto> dtos)
    {
        var dtoList = dtos?.ToList() ?? new List<CreateHangHoaDichVuDto>();
        if (!dtoList.Any())
        {
            return new List<HangHoaDichVuDto>();
        }

        var parentIds = dtoList.Select(d => d.IdParent).Distinct();
        await ValidateParentExistsAsync(parentIds);

        return await base.CreateRangeAsync(dtoList);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateHangHoaDichVuDto dto)
    {
        await ValidateParentExistsAsync(new[] { dto.IdParent });
        return await base.UpdateAsync(id, dto);
    }

    private async Task ValidateParentExistsAsync(IEnumerable<Guid> idParents)
    {
        var distinctIds = idParents.Where(id => id != Guid.Empty).Distinct().ToList();
        if (!distinctIds.Any())
        {
            throw new ArgumentException("IdParent không hợp lệ.");
        }

        var existingHopDongIds = await DbContext.HopDongs
            .Where(h => distinctIds.Contains(h.Id))
            .Select(h => h.Id)
            .ToListAsync();

        var missingIds = distinctIds.Except(existingHopDongIds).ToList();
        if (missingIds.Any())
        {
            throw new ArgumentException($"Hợp đồng với ID '{string.Join(", ", missingIds)}' không tồn tại trong hệ thống.");
        }
    }
}
