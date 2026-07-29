using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs.HangHoa;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class HangHoaService : DbCrudService<HangHoa, HangHoaDto, CreateHangHoaDto, UpdateHangHoaDto>, IHangHoaService
{
    public HangHoaService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public async Task<IEnumerable<HangHoaDto>> GetByIdParentAsync(Guid idParent)
    {
        var entities = await DbContext.HangHoas
            .Include(h => h.XuatXu)
            .Include(h => h.HangSanXuat)
            .Include(h => h.License)
            .Include(h => h.DonViTinh)
            .Where(h => h.IdParent == idParent)
            .ToListAsync();

        return Mapper.Map<IEnumerable<HangHoaDto>>(entities);
    }

    public override async Task<HangHoaDto> CreateAsync(CreateHangHoaDto dto)
    {
        await ValidateParentExistsAsync(new[] { dto.IdParent });
        return await base.CreateAsync(dto);
    }

    public override async Task<IEnumerable<HangHoaDto>> CreateRangeAsync(IEnumerable<CreateHangHoaDto> dtos)
    {
        var dtoList = dtos?.ToList() ?? new List<CreateHangHoaDto>();
        if (!dtoList.Any())
        {
            return new List<HangHoaDto>();
        }

        var parentIds = dtoList.Select(d => d.IdParent).Distinct();
        await ValidateParentExistsAsync(parentIds);

        return await base.CreateRangeAsync(dtoList);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateHangHoaDto dto)
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
