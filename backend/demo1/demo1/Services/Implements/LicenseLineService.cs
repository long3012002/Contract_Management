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

public class LicenseLineService : DbCrudService<HangHoaDichVu, HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>, ILicenseLineService
{
    public LicenseLineService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    protected override IQueryable<HangHoaDichVu> GetQueryable()
    {
        return DbSet.Where(h => h.Loai == LoaiHangHoaDichVu.License);
    }

    protected override HangHoaDichVu CreateEntity(CreateHangHoaDichVuDto dto)
    {
        var entity = base.CreateEntity(dto);
        entity.Loai = LoaiHangHoaDichVu.License;
        return entity;
    }

    protected override void UpdateEntity(HangHoaDichVu entity, UpdateHangHoaDichVuDto dto)
    {
        base.UpdateEntity(entity, dto);
        entity.Loai = LoaiHangHoaDichVu.License;
    }

    public async Task<IEnumerable<HangHoaDichVuDto>> GetByIdParentAsync(Guid idParent)
    {
        var entities = await GetQueryable()
            .Include(h => h.XuatXu)
            .Include(h => h.HangSanXuat)
            .Include(h => h.License)
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

        var existingDotThanhToanIds = await DbContext.DotThanhToans
            .Where(d => distinctIds.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync();

        var existingGoiThauIds = await DbContext.GoiThaus
            .Where(g => distinctIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var validIds = existingHopDongIds
            .Concat(existingDotThanhToanIds)
            .Concat(existingGoiThauIds)
            .Distinct()
            .ToList();

        var missingIds = distinctIds.Except(validIds).ToList();
        if (missingIds.Any())
        {
            throw new ArgumentException($"Hóa đơn / Đợt thanh toán / Hợp đồng với ID '{string.Join(", ", missingIds)}' không tồn tại trong hệ thống.");
        }
    }
}
