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

public class DuAnBudgetService : IDuAnBudgetService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IDuAnSecurityService _securityService;

    public DuAnBudgetService(AppDbContext dbContext, IMapper mapper, IDuAnSecurityService securityService)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _securityService = securityService;
    }

    public async Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto)
    {
        var entity = await _dbContext.DuAns.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "EDIT");

        if (entity.LoaiDuAn != 1)
        {
            throw new InvalidOperationException("Chỉ dự án nguồn mới có thể thực hiện điều chỉnh dự toán.");
        }

        var currentTotalBudget = entity.DuToanPheDuyet + (entity.DieuChinhs?.Sum(d => d.GiaTriDieuChinh) ?? 0m);
        var newTotalBudget = currentTotalBudget + dto.GiaTriDieuChinh;
        if (newTotalBudget < 0)
        {
            throw new InvalidOperationException($"Tổng dự toán sau điều chỉnh ({newTotalBudget:N0} VNĐ) không được âm.");
        }

        // Check against allocated packages
        string sourceIdStr = id.ToString();
        var linkedTrienKhaiIds = await _dbContext.DuAnNguonTrienKhais
            .Where(nk => nk.NguonProjectId != null && EF.Functions.Like(nk.NguonProjectId, $"%{sourceIdStr}%"))
            .Select(nk => nk.TrienKhaiProjectId)
            .ToListAsync();

        var totalPackageValue = await _dbContext.GoiThaus
            .Where(gt => linkedTrienKhaiIds.Contains(gt.DuAnId ?? Guid.Empty))
            .SumAsync(gt => (decimal?)gt.GiaTriGoiThau) ?? 0m;

        if (newTotalBudget < totalPackageValue)
        {
            throw new InvalidOperationException($"Tổng dự toán sau điều chỉnh ({newTotalBudget:N0} VNĐ) không được nhỏ hơn tổng giá trị các gói thầu đã duyệt ({totalPackageValue:N0} VNĐ).");
        }

        var adjustment = new DieuChinhDuAn
        {
            Id = Guid.NewGuid(),
            DuAnId = id,
            GiaTriDieuChinh = dto.GiaTriDieuChinh,
            LyDoDieuChinh = dto.LyDoDieuChinh,
            NgayDieuChinh = DateTime.UtcNow,
            Code = Guid.NewGuid().ToString().Substring(0, 8), // BaseEntity requires Code
            Name = $"Điều chỉnh hạn mức dự án {entity.Name}", // BaseEntity requires Name
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.DieuChinhDuAns.AddAsync(adjustment);
        await _dbContext.SaveChangesAsync();

        // Update all implementation projects linked to this source project
        string idString = id.ToString();
        var allLinks = await _dbContext.DuAnNguonTrienKhais
            .Where(nk => nk.NguonProjectId != null && EF.Functions.Like(nk.NguonProjectId, $"%{idString}%"))
            .ToListAsync();

        var implementationProjectIds = allLinks.Select(nk => nk.TrienKhaiProjectId).Distinct().ToList();

        if (implementationProjectIds.Any())
        {
            var implementationProjects = await _dbContext.DuAns
                .Where(da => implementationProjectIds.Contains(da.Id))
                .ToListAsync();

            var allSourceGuids = allLinks
                .Where(nk => !string.IsNullOrWhiteSpace(nk.NguonProjectId))
                .SelectMany(nk => nk.NguonProjectId!.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

            var sourceProjectsDict = new Dictionary<Guid, DuAn>();
            if (allSourceGuids.Any())
            {
                var sourceProjectsList = await _dbContext.DuAns.Include(da => da.DieuChinhs)
                                                    .Where(da => allSourceGuids.Contains(da.Id))
                                                    .ToListAsync();
                sourceProjectsDict = sourceProjectsList.ToDictionary(sp => sp.Id, sp => sp);
            }

            var goiThauBudgetsDict = await _dbContext.GoiThaus
                .Where(gt => gt.DuAnId.HasValue && implementationProjectIds.Contains(gt.DuAnId.Value))
                .GroupBy(gt => gt.DuAnId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(gt => gt.GiaTriGoiThau));

            foreach (var ip in implementationProjects)
            {
                var link = allLinks.FirstOrDefault(nk => nk.TrienKhaiProjectId == ip.Id);
                var sourceGuids = link?.NguonProjectId?
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();

                if (sourceGuids.Contains(id))
                {
                    decimal totalAggregatedBudget = 0;
                    foreach (var spId in sourceGuids)
                    {
                        if (sourceProjectsDict.TryGetValue(spId, out var sp))
                        {
                            var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                            totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                        }
                    }

                    ip.DuToanPheDuyet = totalAggregatedBudget;

                    decimal goiThauBudgetsSum = 0;
                    if (goiThauBudgetsDict.TryGetValue(ip.Id, out var sum))
                    {
                        goiThauBudgetsSum = sum;
                    }

                    if (totalAggregatedBudget < goiThauBudgetsSum)
                    {
                        throw new InvalidOperationException($"Điều chỉnh ngân sách làm cho tổng ngân sách của dự án triển khai liên kết '{ip.Name}' ({totalAggregatedBudget:N0} VNĐ) không đủ bao phủ các gói thầu đã lập ({goiThauBudgetsSum:N0} VNĐ).");
                    }

                    ip.UpdatedAt = DateTime.UtcNow;
                }
            }
            await _dbContext.SaveChangesAsync();
        }

        return _mapper.Map<DieuChinhDuAnDto>(adjustment);
    }

    public async Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id)
    {
        var entity = await _dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");

        var adjustments = await _dbContext.DieuChinhDuAns
                                         .Where(dc => dc.DuAnId == id)
                                         .OrderByDescending(dc => dc.NgayDieuChinh)
                                         .ToListAsync();
        return _mapper.Map<List<DieuChinhDuAnDto>>(adjustments);
    }
}
