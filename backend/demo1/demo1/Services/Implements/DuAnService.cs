using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Services.Implements;

public class DuAnService : DbCrudService<DuAn, DuAnDto, CreateDuAnDto, UpdateDuAnDto>, IDuAnService
{
    public DuAnService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<PagedResult<DuAnDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<DuAn> query = DbSet.AsNoTracking().Include(da => da.DieuChinhs);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        List<DuAn> items;
        bool isKeyset = TryParseCursor(cursor, out var lastCreatedAt, out var lastId);

        if (isKeyset)
        {
            items = await query
                .Where(item => item.CreatedAt < lastCreatedAt || (item.CreatedAt == lastCreatedAt && item.Id.CompareTo(lastId) < 0))
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(pageSize)
                .ToListAsync();
        }
        else
        {
            items = await query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        string? nextCursor = null;
        if (items.Any())
        {
            var lastItem = items.Last();
            var hasMore = await query
                .Where(item => item.CreatedAt < lastItem.CreatedAt || (item.CreatedAt == lastItem.CreatedAt && item.Id.CompareTo(lastItem.Id) < 0))
                .AnyAsync();
            if (hasMore)
            {
                nextCursor = EncodeCursor(lastItem.CreatedAt, lastItem.Id);
            }
        }

        var dtos = Mapper.Map<List<DuAnDto>>(items);

        return new PagedResult<DuAnDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    public override async Task<IReadOnlyList<DuAnDto>> GetAllItemsAsync()
    {
        var items = await DbSet.Include(da => da.DieuChinhs).ToListAsync();
        return Mapper.Map<List<DuAnDto>>(items);
    }

    public override async Task<DuAnDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs)
                               .FirstOrDefaultAsync(da => da.Id == id);
        return entity is null ? null : Mapper.Map<DuAnDto>(entity);
    }

    public override async Task<DuAnDto> CreateAsync(CreateDuAnDto dto)
    {
        DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc);
        
        var entity = Mapper.Map<DuAn>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        if (dto.LoaiDuAn == 2) // Du an trien khai
        {
            if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
            {
                throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
            }

            // Get source projects
            var sourceProjects = await DbSet.Include(da => da.DieuChinhs)
                                            .Where(da => dto.SourceProjectIds.Contains(da.Id))
                                            .ToListAsync();

            if (sourceProjects.Count != dto.SourceProjectIds.Count)
            {
                throw new ArgumentException("Một số dự án nguồn được chọn không tồn tại.");
            }

            if (sourceProjects.Any(da => da.LoaiDuAn != 1))
            {
                throw new ArgumentException("Chỉ được liên kết đến các dự án nguồn (loại dự án nguồn).");
            }

            // Check if any of these source projects are already linked to an existing implementation project
            var existingImplProjects = await DbSet
                .Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null && da.NguonDuAnIds != "")
                .ToListAsync();

            var usedSourceIds = existingImplProjects
                .SelectMany(da => da.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToHashSet();

            var alreadyAssignedIds = dto.SourceProjectIds.Intersect(usedSourceIds).ToList();
            if (alreadyAssignedIds.Any())
            {
                var firstAssignedId = alreadyAssignedIds.First();
                var sourceProj = sourceProjects.FirstOrDefault(p => p.Id == firstAssignedId);
                var sourceProjName = sourceProj?.Name ?? firstAssignedId.ToString();
                throw new InvalidOperationException($"Dự án nguồn '{sourceProjName}' đã thuộc về một dự án triển khai khác.");
            }

            // Save source project IDs as semicolon separated string
            entity.NguonDuAnIds = string.Join(";", dto.SourceProjectIds.Select(id => id.ToString()));

            // Sum budgets (approved budget + adjustments)
            decimal totalAggregatedBudget = 0;
            foreach (var sp in sourceProjects)
            {
                var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
            }

            entity.DuToanPheDuyet = totalAggregatedBudget;
        }
        else // Du an nguon
        {
            entity.LoaiDuAn = 1;
            entity.NguonDuAnIds = null;
        }

        // Validate unique code
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == entity.Code.ToLower());
        if (exists)
        {
            throw new InvalidOperationException($"Mã dự án '{entity.Code}' đã tồn tại.");
        }

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        return Mapper.Map<DuAnDto>(entity);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateDuAnDto dto)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            return false;
        }

        // Handle source projects update for implementation projects
        if (entity.LoaiDuAn == 2)
        {
            if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
            {
                throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
            }

            var sourceProjects = await DbSet.Include(da => da.DieuChinhs)
                                            .Where(da => dto.SourceProjectIds.Contains(da.Id))
                                            .ToListAsync();

            if (sourceProjects.Count != dto.SourceProjectIds.Count)
            {
                throw new ArgumentException("Một số dự án nguồn được chọn không tồn tại.");
            }

            if (sourceProjects.Any(da => da.LoaiDuAn != 1))
            {
                throw new ArgumentException("Chỉ được liên kết đến các dự án nguồn (loại dự án nguồn).");
            }

            // Check if already assigned to other implementation projects
            var existingOtherImplProjects = await DbSet
                .Where(da => da.LoaiDuAn == 2 && da.Id != id && da.NguonDuAnIds != null && da.NguonDuAnIds != "")
                .ToListAsync();

            var usedSourceIds = existingOtherImplProjects
                .SelectMany(da => da.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToHashSet();

            var alreadyAssignedIds = dto.SourceProjectIds.Intersect(usedSourceIds).ToList();
            if (alreadyAssignedIds.Any())
            {
                var firstAssignedId = alreadyAssignedIds.First();
                var sourceProj = sourceProjects.FirstOrDefault(p => p.Id == firstAssignedId);
                var sourceProjName = sourceProj?.Name ?? firstAssignedId.ToString();
                throw new InvalidOperationException($"Dự án nguồn '{sourceProjName}' đã thuộc về một dự án triển khai khác.");
            }

            entity.NguonDuAnIds = string.Join(";", dto.SourceProjectIds.Select(spId => spId.ToString()));

            // Sum budgets
            decimal totalAggregatedBudget = 0;
            foreach (var sp in sourceProjects)
            {
                var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
            }

            entity.DuToanPheDuyet = totalAggregatedBudget;
            dto.DuToanPheDuyet = totalAggregatedBudget;
        }

        DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc);

        // Prevent direct budget modification for projects
        if (dto.DuToanPheDuyet != entity.DuToanPheDuyet)
        {
            if (entity.LoaiDuAn == 1)
            {
                throw new InvalidOperationException("Dự án nguồn không thể sửa đổi dự toán phê duyệt trực tiếp. Vui lòng sử dụng chức năng điều chỉnh dự án.");
            }
            else
            {
                throw new InvalidOperationException("Dự án triển khai không thể sửa đổi dự toán trực tiếp vì nó được tổng hợp tự động từ các dự án nguồn.");
            }
        }

        Mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return true;
    }

    public async Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        if (entity.LoaiDuAn != 1)
        {
            throw new InvalidOperationException("Chỉ dự án nguồn mới có thể thực hiện điều chỉnh dự toán.");
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

        await DbContext.DieuChinhDuAns.AddAsync(adjustment);
        await DbContext.SaveChangesAsync();

        // Update all implementation projects linked to this source project
        var implementationProjects = await DbSet.Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null).ToListAsync();
        foreach (var ip in implementationProjects)
        {
            var sourceIds = ip.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                           .ToList();
            if (sourceIds.Contains(id))
            {
                // Re-evaluate aggregate budget of this implementation project
                var sourceProjects = await DbSet.Include(da => da.DieuChinhs)
                                                .Where(da => sourceIds.Contains(da.Id))
                                                .ToListAsync();
                decimal totalAggregatedBudget = 0;
                foreach (var sp in sourceProjects)
                {
                    var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                    totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                }

                ip.DuToanPheDuyet = totalAggregatedBudget;
                ip.UpdatedAt = DateTime.UtcNow;
            }
        }
        await DbContext.SaveChangesAsync();

        return Mapper.Map<DieuChinhDuAnDto>(adjustment);
    }

    public async Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id)
    {
        var adjustments = await DbContext.DieuChinhDuAns
                                         .Where(dc => dc.DuAnId == id)
                                         .OrderByDescending(dc => dc.NgayDieuChinh)
                                         .ToListAsync();
        return Mapper.Map<List<DieuChinhDuAnDto>>(adjustments);
    }

    public async Task<DuAnDto> AdvanceStatusAsync(Guid id)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        if (entity.TrangThai >= (int)TrangThaiDuAn.HoanThanh)
        {
            throw new InvalidOperationException("Dự án đã ở trạng thái hoàn thành hoặc cao hơn, không thể chuyển tiếp.");
        }

        entity.TrangThai += 1;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return Mapper.Map<DuAnDto>(entity);
    }

    public async Task<DuAnDto> CloseProjectAsync(Guid id)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        entity.TrangThai = (int)TrangThaiDuAn.HoanThanh;
        entity.DaKetThuc = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return Mapper.Map<DuAnDto>(entity);
    }

    public async Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id)
    {
        var items = await DbContext.GoiThaus
                                   .Where(gt => gt.DuAnId == id)
                                   .ToListAsync();
        return Mapper.Map<List<GoiThauDto>>(items);
    }

    public async Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id)
    {
        var items = await DbContext.HopDongs
                                   .Include(hd => hd.GoiThau)
                                   .Where(hd => hd.GoiThau != null && hd.GoiThau.DuAnId == id)
                                   .ToListAsync();
        return Mapper.Map<List<HopDongDto>>(items);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id)
    {
        var projectIdStr = id.ToString();

        var dieuChinhIds = await DbContext.DieuChinhDuAns
                                          .Where(dc => dc.DuAnId == id)
                                          .Select(dc => dc.Id.ToString())
                                          .ToListAsync();

        var goiThauIds = await DbContext.GoiThaus
                                        .Where(gt => gt.DuAnId == id)
                                        .Select(gt => gt.Id.ToString())
                                        .ToListAsync();

        var hopDongIds = await DbContext.HopDongs
                                        .Include(hd => hd.GoiThau)
                                        .Where(hd => hd.GoiThau != null && hd.GoiThau.DuAnId == id)
                                        .Select(hd => hd.Id.ToString())
                                        .ToListAsync();

        var logs = await DbContext.AuditLogs
                                  .Where(log => 
                                      (log.TableName == nameof(AppDbContext.DuAns) && log.EntityId == projectIdStr) ||
                                      (log.TableName == nameof(AppDbContext.DieuChinhDuAns) && dieuChinhIds.Contains(log.EntityId)) ||
                                      (log.TableName == nameof(AppDbContext.GoiThaus) && goiThauIds.Contains(log.EntityId)) ||
                                      (log.TableName == nameof(AppDbContext.HopDongs) && hopDongIds.Contains(log.EntityId))
                                  )
                                  .OrderByDescending(log => log.Timestamp)
                                  .ToListAsync();

        return logs;
    }
}

