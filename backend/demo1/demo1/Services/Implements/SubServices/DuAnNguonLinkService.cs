using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces.SubServices;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements.SubServices;

public class DuAnNguonLinkService : IDuAnNguonLinkService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IDuAnSecurityService _securityService;

    public DuAnNguonLinkService(AppDbContext dbContext, IMapper mapper, IDuAnSecurityService securityService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _securityService = securityService;
    }

    public async Task<IReadOnlyList<DuAnNguonSummaryDto>> GetSourceProjectsByProjectIdAsync(Guid id)
    {
        var entity = await _dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");

        var gopLinks = await _dbContext.DuAnGopLinks
            .Include(g => g.SourceDuAn)
            .AsNoTracking()
            .Where(g => g.TargetDuAnId == id)
            .ToListAsync();

        var sourceEntities = gopLinks.Select(g => g.SourceDuAn).Where(s => s != null).ToList();
        return _mapper.Map<List<DuAnNguonSummaryDto>>(sourceEntities);
    }

    public async Task PopulateSourceProjectsAsync(List<DuAnDto> dtos)
    {
        if (dtos == null || !dtos.Any()) return;

        var projectIds = dtos.Select(d => d.Id).ToList();

        var gopLinks = await _dbContext.DuAnGopLinks
            .Include(g => g.SourceDuAn)
            .Include(g => g.TargetDuAn)
            .Include(g => g.NguoiThucHien)
            .AsNoTracking()
            .Where(g => projectIds.Contains(g.TargetDuAnId) || projectIds.Contains(g.SourceDuAnId))
            .ToListAsync();

        var linksByTarget = gopLinks.GroupBy(g => g.TargetDuAnId).ToDictionary(g => g.Key, g => g.ToList());
        var linksBySource = gopLinks.ToDictionary(g => g.SourceDuAnId, g => g);

        foreach (var dto in dtos)
        {
            if (linksByTarget.TryGetValue(dto.Id, out var targetLinks))
            {
                dto.DanhSachDuAnDaGop = targetLinks.Select(l => new DuAnGopLinkDto
                {
                    Id = l.Id,
                    SourceDuAnId = l.SourceDuAnId,
                    SourceMaDuAn = l.SourceDuAn?.Code ?? "",
                    SourceTenDuAn = l.SourceDuAn?.Name ?? "",
                    TargetDuAnId = l.TargetDuAnId,
                    TargetMaDuAn = l.TargetDuAn?.Code ?? "",
                    TargetTenDuAn = l.TargetDuAn?.Name ?? "",
                    NgayGop = l.NgayGop,
                    NguoiThucHienId = l.NguoiThucHienId,
                    NguoiThucHienName = l.NguoiThucHien?.FullName ?? "",
                    DuToanLucGop = l.DuToanLucGop,
                    GhiChu = l.GhiChu
                }).ToList();
            }

            if (linksBySource.TryGetValue(dto.Id, out var sourceLink))
            {
                dto.ThongTinGopVao = new DuAnGopLinkDto
                {
                    Id = sourceLink.Id,
                    SourceDuAnId = sourceLink.SourceDuAnId,
                    SourceMaDuAn = sourceLink.SourceDuAn?.Code ?? "",
                    SourceTenDuAn = sourceLink.SourceDuAn?.Name ?? "",
                    TargetDuAnId = sourceLink.TargetDuAnId,
                    TargetMaDuAn = sourceLink.TargetDuAn?.Code ?? "",
                    TargetTenDuAn = sourceLink.TargetDuAn?.Name ?? "",
                    NgayGop = sourceLink.NgayGop,
                    NguoiThucHienId = sourceLink.NguoiThucHienId,
                    NguoiThucHienName = sourceLink.NguoiThucHien?.FullName ?? "",
                    DuToanLucGop = sourceLink.DuToanLucGop,
                    GhiChu = sourceLink.GhiChu
                };
            }
        }
    }

    public async Task<HashSet<Guid>> GetLinkedSourceProjectIdsAsync(Guid? excludeTrienKhaiProjectId = null)
    {
        var query = _dbContext.DuAnGopLinks.AsQueryable();
        if (excludeTrienKhaiProjectId.HasValue)
        {
            query = query.Where(g => g.TargetDuAnId != excludeTrienKhaiProjectId.Value);
        }

        var sourceIds = await query.Select(g => g.SourceDuAnId).ToListAsync();
        return sourceIds.ToHashSet();
    }
}
