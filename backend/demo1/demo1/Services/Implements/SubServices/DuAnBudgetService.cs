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
        var entity = await _dbContext.DuAns.FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "UPDATE");

        if (dto.GiaTriDieuChinh == 0)
        {
            throw new ArgumentException("Số tiền điều chỉnh phải khác 0.");
        }

        if (string.IsNullOrWhiteSpace(dto.LyDoDieuChinh))
        {
            throw new ArgumentException("Vui lòng nhập lý do điều chỉnh.");
        }

        entity.DuToanPheDuyet += dto.GiaTriDieuChinh;
        entity.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return new DieuChinhDuAnDto
        {
            Id = Guid.NewGuid(),
            DuAnId = id,
            GiaTriDieuChinh = dto.GiaTriDieuChinh,
            LyDoDieuChinh = dto.LyDoDieuChinh,
            NgayDieuChinh = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id)
    {
        var entity = await _dbContext.DuAns.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");

        return new List<DieuChinhDuAnDto>();
    }
}
