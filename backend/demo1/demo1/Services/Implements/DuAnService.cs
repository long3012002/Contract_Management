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
using Microsoft.Extensions.Logging;
using demo1.Data;

namespace demo1.Services.Implements;

public class DuAnService : DbCrudService<DuAn, DuAnDto, CreateDuAnDto, UpdateDuAnDto>, IDuAnService
{
    private readonly ILogger<DuAnService> _logger;

    public DuAnService(AppDbContext dbContext, IMapper mapper, ILogger<DuAnService> logger) : base(dbContext, mapper)
    {
        _logger = logger;
    }

    public override async Task<PagedResult<DuAnDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<DuAn> query = DbSet.AsNoTracking()
                .Include(da => da.DieuChinhs)
                .Include(da => da.NhomDuAn)
                .Include(da => da.PhanLoaiDuAn);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllAsync của DuAnService.");
            throw;
        }
    }

    public async Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto)
    {
        try
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
                Code = Guid.NewGuid().ToString().Substring(0, 8),
                Name = $"Điều chỉnh hạn mức dự án {entity.Name}",
                CreatedAt = DateTime.UtcNow
            };

            await DbContext.DieuChinhDuAns.AddAsync(adjustment);
            await DbContext.SaveChangesAsync();

            var implementationProjects = await DbSet.Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null).ToListAsync();
            foreach (var ip in implementationProjects)
            {
                var sourceIds = ip.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                               .ToList();
                if (sourceIds.Contains(id))
                {
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

                    var goiThauBudgetsSum = await DbContext.GoiThaus
                        .Where(gt => gt.DuAnId == ip.Id)
                        .SumAsync(gt => gt.GiaTriGoiThau);
                    if (totalAggregatedBudget < goiThauBudgetsSum)
                    {
                        throw new InvalidOperationException($"Điều chỉnh ngân sách làm cho tổng ngân sách của dự án triển khai liên kết '{ip.Name}' ({totalAggregatedBudget:N0} VNĐ) không đủ bao phủ các gói thầu đã lập ({goiThauBudgetsSum:N0} VNĐ).");
                    }

                    ip.UpdatedAt = DateTime.UtcNow;
                }
            }
            await DbContext.SaveChangesAsync();

            return Mapper.Map<DieuChinhDuAnDto>(adjustment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong AdjustBudgetAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id)
    {
        try
        {
            var adjustments = await DbContext.DieuChinhDuAns
                                             .Where(dc => dc.DuAnId == id)
                                             .OrderByDescending(dc => dc.NgayDieuChinh)
                                             .ToListAsync();
            return Mapper.Map<List<DieuChinhDuAnDto>>(adjustments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAdjustmentsAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<DuAnDto> AdvanceStatusAsync(Guid id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong AdvanceStatusAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<DuAnDto> CloseProjectAsync(Guid id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CloseProjectAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id)
    {
        try
        {
            var items = await DbContext.GoiThaus
                                       .Where(gt => gt.DuAnId == id)
                                       .ToListAsync();
            return Mapper.Map<List<GoiThauDto>>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetGoiThausByProjectIdAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id)
    {
        try
        {
            var items = await DbContext.HopDongs
                                       .Include(hd => hd.GoiThau)
                                       .Where(hd => hd.GoiThau != null && hd.GoiThau.DuAnId == id)
                                       .ToListAsync();
            return Mapper.Map<List<HopDongDto>>(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetHopDongsByProjectIdAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id)
    {
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAuditLogsByProjectIdAsync cho DuAnId {Id}.", id);
            throw;
        }
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await DbSet.FirstOrDefaultAsync(da => da.Id == id);
            if (entity is null)
            {
                return false;
            }

            var hopDongs = await DbContext.HopDongs
                .Where(hd => hd.DuAnId == id || (hd.GoiThau != null && hd.GoiThau.DuAnId == id))
                .ToListAsync();

            if (hopDongs.Any())
            {
                DbContext.HopDongs.RemoveRange(hopDongs);
            }

            var goiThaus = await DbContext.GoiThaus
                .Where(gt => gt.DuAnId == id)
                .ToListAsync();

            if (goiThaus.Any())
            {
                DbContext.GoiThaus.RemoveRange(goiThaus);
            }

            var dieuChinhs = await DbContext.DieuChinhDuAns
                .Where(dc => dc.DuAnId == id)
                .ToListAsync();

            if (dieuChinhs.Any())
            {
                DbContext.DieuChinhDuAns.RemoveRange(dieuChinhs);
            }

            DbSet.Remove(entity);
            await DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong DeleteAsync của DuAnService cho ID {Id}.", id);
            throw;
        }
    }
}

