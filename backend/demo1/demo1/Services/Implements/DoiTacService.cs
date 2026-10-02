using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class DoiTacService : DbCrudService<DoiTac, DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>, IDoiTacService
{
    public DoiTacService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<PagedResult<DoiTacDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null, bool? isDeleted = null)
    {
        var result = await base.GetAllAsync(search, page, pageSize, cursor, isDeleted);
        if (result.Items != null && result.Items.Any())
        {
            var doiTacIds = result.Items.Select(x => x.Id).ToList();

            var directContracts = DbContext.HopDongs
                .Where(h => !h.IsDeleted && h.NhaThauId.HasValue && doiTacIds.Contains(h.NhaThauId.Value))
                .Select(h => new { DoiTacId = h.NhaThauId!.Value, HopDongId = h.Id });

            var jointContracts = DbContext.NhaThauGoiThaus
                .Where(nt => doiTacIds.Contains(nt.NhaThauId) && nt.HopDong != null && !nt.HopDong.IsDeleted)
                .Select(nt => new { DoiTacId = nt.NhaThauId, HopDongId = nt.HopDongId });

            var contractCounts = await directContracts
                .Concat(jointContracts)
                .Distinct()
                .GroupBy(x => x.DoiTacId)
                .Select(g => new { DoiTacId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DoiTacId, x => x.Count);

            foreach (var item in result.Items)
            {
                item.ContractCount = contractCounts.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }
        return result;
    }

    public override async Task<DoiTacDto?> GetByIdAsync(Guid id)
    {
        var dto = await base.GetByIdAsync(id);
        if (dto != null)
        {
            var direct = DbContext.HopDongs
                .Where(h => !h.IsDeleted && h.NhaThauId == id)
                .Select(h => h.Id);

            var joint = DbContext.NhaThauGoiThaus
                .Where(nt => nt.NhaThauId == id && nt.HopDong != null && !nt.HopDong.IsDeleted)
                .Select(nt => nt.HopDongId);

            dto.ContractCount = await direct.Concat(joint).Distinct().CountAsync();
        }
        return dto;
    }
}
