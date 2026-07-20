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

public class CongViecGoiThauService
    : DbCrudDetailService<CongViecGoiThau, CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>, ICongViecGoiThauService
{
    public CongViecGoiThauService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<CongViecGoiThauDto> CreateAsync(CreateCongViecGoiThauDto dto)
    {
        var goiThauExists = await DbContext.GoiThaus.AnyAsync(g => g.Id == dto.GoiThauId);
        if (!goiThauExists)
        {
            throw new KeyNotFoundException($"Không tìm thấy gói thầu với ID '{dto.GoiThauId}'.");
        }

        var entity = Mapper.Map<CongViecGoiThau>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(entity.Code))
        {
            entity.Code = $"CVGT-{dto.Stt:D2}-{entity.Id.ToString().Substring(0, 6)}";
        }

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            entity.Name = entity.TenTaiLieu;
        }

        if (string.IsNullOrWhiteSpace(entity.Description))
        {
            entity.Description = entity.GhiChu;
        }

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        return Mapper.Map<CongViecGoiThauDto>(entity);
    }

    public override async Task<IEnumerable<CongViecGoiThauDto>> CreateRangeAsync(IEnumerable<CreateCongViecGoiThauDto> dtos)
    {
        var dtoList = dtos.ToList();
        if (!dtoList.Any())
        {
            return Enumerable.Empty<CongViecGoiThauDto>();
        }

        var goiThauIds = dtoList.Select(d => d.GoiThauId).Distinct().ToList();
        var existingGoiThauIds = await DbContext.GoiThaus
            .Where(g => goiThauIds.Contains(g.Id))
            .Select(g => g.Id)
            .ToListAsync();

        var missingGoiThauId = goiThauIds.FirstOrDefault(id => !existingGoiThauIds.Contains(id));
        if (missingGoiThauId != Guid.Empty && !existingGoiThauIds.Contains(missingGoiThauId))
        {
            throw new KeyNotFoundException($"Không tìm thấy gói thầu với ID '{missingGoiThauId}'.");
        }

        var entities = new List<CongViecGoiThau>();
        var now = DateTime.UtcNow;

        foreach (var dto in dtoList)
        {
            var entity = Mapper.Map<CongViecGoiThau>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = now;

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                entity.Code = $"CVGT-{dto.Stt:D2}-{entity.Id.ToString().Substring(0, 6)}";
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                entity.Name = entity.TenTaiLieu;
            }

            if (string.IsNullOrWhiteSpace(entity.Description))
            {
                entity.Description = entity.GhiChu;
            }

            entities.Add(entity);
        }

        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();

        return Mapper.Map<List<CongViecGoiThauDto>>(entities);
    }


    public override async Task<bool> UpdateAsync(Guid id, UpdateCongViecGoiThauDto dto)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        if (dto.GoiThauId.HasValue && dto.GoiThauId.Value != entity.GoiThauId)
        {
            var goiThauExists = await DbContext.GoiThaus.AnyAsync(g => g.Id == dto.GoiThauId.Value);
            if (!goiThauExists)
            {
                throw new KeyNotFoundException($"Không tìm thấy gói thầu với ID '{dto.GoiThauId.Value}'.");
            }
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            entity.Name = entity.TenTaiLieu;
        }

        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<CongViecGoiThauReportDto> GetReportByGoiThauIdAsync(Guid idGoiThau)
    {
        var goiThau = await DbContext.GoiThaus
            .Include(g => g.DuAn)
            .Include(g => g.CongViecGoiThaus)
            .FirstOrDefaultAsync(g => g.Id == idGoiThau);

        if (goiThau == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy gói thầu với ID '{idGoiThau}'.");
        }

        var congViecs = goiThau.CongViecGoiThaus
            .OrderBy(c => c.Stt)
            .ThenBy(c => c.CreatedAt)
            .ToList();

        var congViecDtos = Mapper.Map<List<CongViecGoiThauDto>>(congViecs);

        int completed = congViecs.Count(c => c.TinhTrang != null && c.TinhTrang.Equals("Đã xong", StringComparison.OrdinalIgnoreCase));
        int inProgress = congViecs.Count(c => c.TinhTrang != null && !c.TinhTrang.Equals("Đã xong", StringComparison.OrdinalIgnoreCase));

        return new CongViecGoiThauReportDto
        {
            GoiThauId = goiThau.Id,
            TenGoiThau = goiThau.Name,
            MaGoiThau = goiThau.Code,
            TenDuAn = goiThau.DuAn?.Name,
            GiaTriGoiThau = goiThau.GiaTriGoiThau,
            CongViecs = congViecDtos,
            TongSoCongViec = congViecs.Count,
            SoCongViecDaHoanThanh = completed,
            SoCongViecDangThucHien = inProgress
        };
    }
}
