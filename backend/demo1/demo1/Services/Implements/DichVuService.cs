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

public class DichVuService : DbCrudService<HangHoaDichVu, HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>, IDichVuService
{
    public DichVuService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    protected override IQueryable<HangHoaDichVu> GetQueryable()
    {
        return DbSet.Where(h => h.Loai == LoaiHangHoaDichVu.DichVu);
    }

    protected override HangHoaDichVu CreateEntity(CreateHangHoaDichVuDto dto)
    {
        var entity = base.CreateEntity(dto);
        entity.Loai = LoaiHangHoaDichVu.DichVu;
        return entity;
    }

    protected override void UpdateEntity(HangHoaDichVu entity, UpdateHangHoaDichVuDto dto)
    {
        base.UpdateEntity(entity, dto);
        entity.Loai = LoaiHangHoaDichVu.DichVu;
    }

    public async Task<IEnumerable<HangHoaDichVuDto>> GetByIdParentAsync(Guid idParent)
    {
        var entities = await GetQueryable()
            .Include(h => h.DonViTinh)
            .Where(h => h.IdParent == idParent)
            .ToListAsync();

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

    public override Task<bool> SoftDeleteAsync(Guid id)
    {
        return SoftDeleteAsync(new[] { id });
    }

    public override async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await DbSet.Where(h => idList.Contains(h.Id) && !h.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var userId = DbContext.CurrentUserService?.GetUserId();
        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedByUserId = userId;
            entity.UpdatedAt = now;
        }

        var childItems = await DbSet.Where(h => idList.Contains(h.IdParent)).ToListAsync();
        foreach (var child in childItems)
        {
            child.IsDeleted = true;
            child.DeletedAt = now;
            child.DeletedByUserId = userId;
        }

        await DbContext.SaveChangesAsync();
        return true;
    }

    public override Task<bool> RestoreAsync(Guid id)
    {
        return RestoreAsync(new[] { id });
    }

    public override async Task<bool> RestoreAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await DbSet.IgnoreQueryFilters().Where(e => idList.Contains(e.Id) && e.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;
            entity.UpdatedAt = now;
        }

        var childItems = await DbSet.IgnoreQueryFilters().Where(h => idList.Contains(h.IdParent) && h.IsDeleted).ToListAsync();
        foreach (var child in childItems)
        {
            child.IsDeleted = false;
            child.DeletedAt = null;
            child.DeletedByUserId = null;
        }

        await DbContext.SaveChangesAsync();
        return true;
    }
}
