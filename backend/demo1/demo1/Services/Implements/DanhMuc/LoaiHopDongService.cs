using demo1.DTOs;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class LoaiHopDongService : DbCrudService<LoaiHopDong, LoaiHopDongDto, CreateLoaiHopDongDto, UpdateLoaiHopDongDto>, ILoaiHopDongService
{
    public LoaiHopDongService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateLoaiHopDongDto dto)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null && entity.Code == "01")
        {
            throw new InvalidOperationException("Không thể sửa loại hợp đồng mặc định (Bảo trì).");
        }
        return await base.UpdateAsync(id, dto);
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null && entity.Code == "01")
        {
            throw new InvalidOperationException("Không thể xóa loại hợp đồng mặc định (Bảo trì).");
        }
        return await base.DeleteAsync(id);
    }

    public override async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var entities = await DbSet.Where(e => ids.Contains(e.Id)).ToListAsync();
        if (entities.Any(e => e.Code == "01"))
        {
            throw new InvalidOperationException("Không thể xóa loại hợp đồng mặc định (Bảo trì).");
        }
        return await base.SoftDeleteAsync(ids);
    }
}
