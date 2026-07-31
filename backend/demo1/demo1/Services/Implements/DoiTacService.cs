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

    public override async Task<PagedResult<DoiTacDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        var result = await base.GetAllAsync(search, page, pageSize, cursor);
        if (result.Items != null && result.Items.Any())
        {
            var doiTacIds = result.Items.Select(x => x.Id).ToList();
            var contractCounts = await DbContext.HopDongs
                .Where(h => h.NhaThauId != null && doiTacIds.Contains(h.NhaThauId.Value))
                .GroupBy(h => h.NhaThauId)
                .Select(g => new { DoiTacId = g.Key!.Value, Count = g.Count() })
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
            dto.ContractCount = await DbContext.HopDongs.CountAsync(h => h.NhaThauId == id);
        }
        return dto;
    }
}
