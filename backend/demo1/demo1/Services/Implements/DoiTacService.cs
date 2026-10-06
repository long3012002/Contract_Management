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

    private static string NormalizeTaxCode(string? taxCode)
    {
        if (string.IsNullOrWhiteSpace(taxCode)) return string.Empty;
        return new string(taxCode.Where(char.IsDigit).ToArray());
    }

    public async Task<DoiTacDto?> CheckTaxCodeAsync(string taxCode, Guid? excludeId = null)
    {
        var cleanTaxCode = NormalizeTaxCode(taxCode);
        if (string.IsNullOrWhiteSpace(cleanTaxCode) || cleanTaxCode.Length < 10)
        {
            return null;
        }

        var prefix10 = cleanTaxCode.Substring(0, 10);

        var query = DbContext.DoiTacs
            .Where(d => !d.IsDeleted && !string.IsNullOrEmpty(d.TaxCode));

        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
        {
            query = query.Where(d => d.Id != excludeId.Value);
        }

        var candidates = await query.ToListAsync();

        var matched = candidates.FirstOrDefault(d =>
        {
            var dbClean = NormalizeTaxCode(d.TaxCode);
            if (string.IsNullOrEmpty(dbClean)) return false;
            return dbClean == cleanTaxCode ||
                   (dbClean.Length >= 10 && dbClean.Substring(0, 10) == prefix10);
        });

        return matched == null ? null : Mapper.Map<DoiTacDto>(matched);
    }

    public override async Task<DoiTacDto> CreateAsync(CreateDoiTacDto dto)
    {
        dto.Code = demo1.Validator.CodePrefixValidator.FormatDoiTacCode(dto.Code);

        if (!string.IsNullOrWhiteSpace(dto.TaxCode))
        {
            var existing = await CheckTaxCodeAsync(dto.TaxCode);
            if (existing != null)
            {
                throw new InvalidOperationException($"Mã số thuế '{dto.TaxCode}' đã tồn tại cho nhà thầu '{existing.Name}' ({existing.Code}).");
            }
        }

        return await base.CreateAsync(dto);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateDoiTacDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.TaxCode))
        {
            var existing = await CheckTaxCodeAsync(dto.TaxCode, id);
            if (existing != null)
            {
                throw new InvalidOperationException($"Mã số thuế '{dto.TaxCode}' đã tồn tại cho nhà thầu '{existing.Name}' ({existing.Code}).");
            }
        }

        return await base.UpdateAsync(id, dto);
    }
}
