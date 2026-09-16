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

        var link = await _dbContext.DuAnNguonTrienKhais
            .AsNoTracking()
            .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == id);

        if (link?.NguonProjectId == null || string.IsNullOrWhiteSpace(link.NguonProjectId))
            return new List<DuAnNguonSummaryDto>();

        var sourceGuids = link.NguonProjectId
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(Guid.Parse)
            .ToList();

        var sourceEntities = await _dbContext.DuAns
            .AsNoTracking()
            .Where(da => sourceGuids.Contains(da.Id))
            .Include(da => da.DieuChinhs)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .ToListAsync();

        return _mapper.Map<List<DuAnNguonSummaryDto>>(sourceEntities);
    }

    public async Task PopulateSourceProjectsAsync(List<DuAnDto> dtos)
    {
        var implProjectIds = dtos.Where(d => d.LoaiDuAn == 2).Select(d => d.Id).ToList();
        if (!implProjectIds.Any())
            return;

        var links = await _dbContext.DuAnNguonTrienKhais
            .AsNoTracking()
            .Where(nk => implProjectIds.Contains(nk.TrienKhaiProjectId) && !string.IsNullOrWhiteSpace(nk.NguonProjectId))
            .ToListAsync();

        var allSourceGuids = links
            .SelectMany(nk => nk.NguonProjectId!.Split(';', StringSplitOptions.RemoveEmptyEntries))
            .Select(Guid.Parse)
            .Distinct()
            .ToList();

        var sourceEntitiesDict = new Dictionary<Guid, DuAn>();
        if (allSourceGuids.Any())
        {
            var sourceEntities = await _dbContext.DuAns
                .AsNoTracking()
                .Where(da => allSourceGuids.Contains(da.Id))
                .Include(da => da.DieuChinhs)
                .Include(da => da.NhomDuAn)
                .Include(da => da.PhanLoaiDuAn)
                .ToListAsync();
            sourceEntitiesDict = sourceEntities.ToDictionary(s => s.Id, s => s);
        }

        var linksDict = links.ToDictionary(l => l.TrienKhaiProjectId, l => l.NguonProjectId);

        foreach (var dto in dtos)
        {
            if (dto.LoaiDuAn == 2 && linksDict.TryGetValue(dto.Id, out var nguonIdStr) && !string.IsNullOrWhiteSpace(nguonIdStr))
            {
                var sGuids = nguonIdStr.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse);
                var matchedSources = sGuids.Where(g => sourceEntitiesDict.ContainsKey(g)).Select(g => sourceEntitiesDict[g]).ToList();
                dto.SourceProjects = _mapper.Map<List<DuAnNguonSummaryDto>>(matchedSources);
            }
            else
            {
                dto.SourceProjects = new List<DuAnNguonSummaryDto>();
            }
        }
    }

    public async Task<HashSet<Guid>> GetLinkedSourceProjectIdsAsync(Guid? excludeTrienKhaiProjectId = null)
    {
        var activeTrienKhaiQuery = _dbContext.DuAns.Where(da => !da.IsDeleted && da.LoaiDuAn == 2);
        if (excludeTrienKhaiProjectId.HasValue)
        {
            activeTrienKhaiQuery = activeTrienKhaiQuery.Where(da => da.Id != excludeTrienKhaiProjectId.Value);
        }

        var activeTrienKhaiIds = activeTrienKhaiQuery.Select(da => da.Id);

        var links = await _dbContext.DuAnNguonTrienKhais
            .Where(nk => activeTrienKhaiIds.Contains(nk.TrienKhaiProjectId) && !string.IsNullOrWhiteSpace(nk.NguonProjectId))
            .Select(nk => nk.NguonProjectId)
            .ToListAsync();

        var result = new HashSet<Guid>();
        foreach (var nguonStr in links)
        {
            if (string.IsNullOrWhiteSpace(nguonStr)) continue;
            var parts = nguonStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (Guid.TryParse(part.Trim(), out var parsedId))
                {
                    result.Add(parsedId);
                }
            }
        }

        return result;
    }
}
