using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Hubs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class CongViecGoiThauService
    : DbCrudDetailService<CongViecGoiThau, CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>, ICongViecGoiThauService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public CongViecGoiThauService(
        AppDbContext dbContext,
        IMapper mapper,
        IHubContext<NotificationHub> hubContext) : base(dbContext, mapper)
    {
        _hubContext = hubContext;
    }

    public override async Task<CongViecGoiThauDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet.AsNoTracking()
            .Include(e => e.NguoiLienQuans)
                .ThenInclude(n => n.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity == null) return null;
        return Mapper.Map<CongViecGoiThauDto>(entity);
    }

    public override async Task<IEnumerable<CongViecGoiThauDto>> GetByParentIdAsync(Guid parentId)
    {
        var entities = await DbSet.AsNoTracking()
            .Include(e => e.NguoiLienQuans)
                .ThenInclude(n => n.User)
            .Where(e => e.GoiThauId == parentId)
            .OrderBy(e => e.Stt)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync();

        return Mapper.Map<List<CongViecGoiThauDto>>(entities);
    }

    public override async Task<PagedResult<CongViecGoiThauDto>> GetByParentIdPagedAsync(
        Guid parentId, string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<CongViecGoiThau> query = DbSet.AsNoTracking()
            .Include(e => e.NguoiLienQuans)
                .ThenInclude(n => n.User)
            .Where(e => e.GoiThauId == parentId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        List<CongViecGoiThau> items = await query
            .OrderBy(item => item.Stt)
            .ThenByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = Mapper.Map<List<CongViecGoiThauDto>>(items);

        return new PagedResult<CongViecGoiThauDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
        };
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

        // Handle NguoiLienQuans
        if (dto.NguoiLienQuanIds != null && dto.NguoiLienQuanIds.Any())
        {
            var validUserIds = await DbContext.Users
                .Where(u => dto.NguoiLienQuanIds.Contains(u.Id))
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in validUserIds)
            {
                entity.NguoiLienQuans.Add(new CongViecNguoiLienQuan
                {
                    Id = Guid.NewGuid(),
                    CongViecGoiThauId = entity.Id,
                    UserId = userId,
                    Code = $"NLQ-{Guid.NewGuid():N}",
                    Name = $"Stakeholder-{userId}",
                    TrangThaiXacNhan = "Pending",
                    HanXacNhanAt = entity.CreatedAt.AddHours(24),
                    CreatedAt = entity.CreatedAt
                });
            }
        }

        await DbSet.AddAsync(entity);

        // Auto-reorder STT for tasks in package
        await NormalizeAndReorderTasksSttAsync(dto.GoiThauId, entity.Id, dto.Stt);

        await DbContext.SaveChangesAsync();

        // Push notifications to stakeholders
        await SendStakeholderNotificationsAsync(new List<CongViecGoiThau> { entity });

        return await GetByIdAsync(entity.Id) ?? Mapper.Map<CongViecGoiThauDto>(entity);
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

        // Collect all distinct user IDs
        var allUserIds = dtoList.Where(d => d.NguoiLienQuanIds != null)
            .SelectMany(d => d.NguoiLienQuanIds!)
            .Distinct()
            .ToList();

        var validUsers = await DbContext.Users
            .Where(u => allUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

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

            if (dto.NguoiLienQuanIds != null && dto.NguoiLienQuanIds.Any())
            {
                foreach (var userId in dto.NguoiLienQuanIds)
                {
                    if (validUsers.ContainsKey(userId))
                    {
                        entity.NguoiLienQuans.Add(new CongViecNguoiLienQuan
                        {
                            Id = Guid.NewGuid(),
                            CongViecGoiThauId = entity.Id,
                            UserId = userId,
                            Code = $"NLQ-{Guid.NewGuid():N}",
                            Name = $"Stakeholder-{userId}",
                            TrangThaiXacNhan = "Pending",
                            HanXacNhanAt = now.AddHours(24),
                            CreatedAt = now
                        });
                    }
                }
            }

            entities.Add(entity);
        }

        await DbSet.AddRangeAsync(entities);

        // Auto-reorder STT for each package
        foreach (var packageId in goiThauIds)
        {
            await NormalizeAndReorderTasksSttAsync(packageId);
        }

        await DbContext.SaveChangesAsync();

        // Push notifications to stakeholders
        await SendStakeholderNotificationsAsync(entities);

        var createdIds = entities.Select(e => e.Id).ToList();
        var resultEntities = await DbSet.AsNoTracking()
            .Include(e => e.NguoiLienQuans)
                .ThenInclude(n => n.User)
            .Where(e => createdIds.Contains(e.Id))
            .OrderBy(e => e.Stt)
            .ToListAsync();

        return Mapper.Map<List<CongViecGoiThauDto>>(resultEntities);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateCongViecGoiThauDto dto)
    {
        var entity = await DbSet
            .Include(e => e.NguoiLienQuans)
            .FirstOrDefaultAsync(e => e.Id == id);

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

        if (dto.NguoiLienQuanIds != null)
        {
            var existingUserIds = entity.NguoiLienQuans.Select(n => n.UserId).ToList();
            var newUserIds = dto.NguoiLienQuanIds.Distinct().ToList();

            // Remove unselected stakeholders
            var toRemove = entity.NguoiLienQuans.Where(n => !newUserIds.Contains(n.UserId)).ToList();
            foreach (var rem in toRemove)
            {
                DbContext.CongViecNguoiLienQuans.Remove(rem);
            }

            // Add newly selected stakeholders
            var toAddUserIds = newUserIds.Where(uid => !existingUserIds.Contains(uid)).ToList();
            if (toAddUserIds.Any())
            {
                var validAddUsers = await DbContext.Users
                    .Where(u => toAddUserIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var addUserId in validAddUsers)
                {
                    entity.NguoiLienQuans.Add(new CongViecNguoiLienQuan
                    {
                        Id = Guid.NewGuid(),
                        CongViecGoiThauId = entity.Id,
                        UserId = addUserId,
                        Code = $"NLQ-{Guid.NewGuid():N}",
                        Name = $"Stakeholder-{addUserId}",
                        TrangThaiXacNhan = "Pending",
                        HanXacNhanAt = DateTime.UtcNow.AddHours(24),
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // Auto-reorder STT if changed
        await NormalizeAndReorderTasksSttAsync(entity.GoiThauId, entity.Id, dto.Stt);

        try
        {
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            var exists = await DbSet.AsNoTracking().AnyAsync(e => e.Id == id);
            if (!exists)
            {
                throw new KeyNotFoundException($"Không tìm thấy công việc gói thầu với ID '{id}' (có thể đã bị xóa).");
            }
            throw;
        }

        return true;
    }

    public async Task<bool> ConfirmCongViecAsync(Guid id, Guid userId)
    {
        var record = await DbContext.CongViecNguoiLienQuans
            .FirstOrDefaultAsync(n => n.CongViecGoiThauId == id && n.UserId == userId);

        if (record == null) return false;

        record.TrangThaiXacNhan = "Confirmed";
        record.XacNhanAt = DateTime.UtcNow;
        record.LoaiXacNhan = "DirectConfirm";
        record.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<CongViecGoiThauReportDto> GetReportByGoiThauIdAsync(Guid idGoiThau)
    {
        var goiThau = await DbContext.GoiThaus
            .Include(g => g.DuAn)
            .Include(g => g.CongViecGoiThaus)
                .ThenInclude(c => c.NguoiLienQuans)
                    .ThenInclude(n => n.User)
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

        int completed = congViecs.Count(c => c.TinhTrang != null && (c.TinhTrang.Equals("Đã xong", StringComparison.OrdinalIgnoreCase) || c.TinhTrang.Equals("Đã hoàn thành", StringComparison.OrdinalIgnoreCase) || c.TinhTrang.Equals("Đã ký", StringComparison.OrdinalIgnoreCase)));
        int inProgress = congViecs.Count(c => c.TinhTrang != null && (c.TinhTrang.Equals("Đang thực hiện", StringComparison.OrdinalIgnoreCase)));

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

    private async Task NormalizeAndReorderTasksSttAsync(Guid goiThauId, Guid? targetEntityId = null, int? newTargetStt = null)
    {
        var tasks = await DbSet
            .Where(t => t.GoiThauId == goiThauId)
            .OrderBy(t => t.Stt)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        if (!tasks.Any()) return;

        if (targetEntityId.HasValue && newTargetStt.HasValue)
        {
            var targetTask = tasks.FirstOrDefault(t => t.Id == targetEntityId.Value);
            if (targetTask != null)
            {
                tasks.Remove(targetTask);
                int insertIndex = Math.Clamp(newTargetStt.Value - 1, 0, tasks.Count);
                tasks.Insert(insertIndex, targetTask);
            }
        }

        // Re-index all tasks sequentially: 1, 2, 3...
        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i].Stt = i + 1;
        }
    }

    private async Task SendStakeholderNotificationsAsync(List<CongViecGoiThau> tasks)
    {
        foreach (var task in tasks)
        {
            if (task.NguoiLienQuans == null || !task.NguoiLienQuans.Any()) continue;

            var userIds = task.NguoiLienQuans.Select(n => n.UserId).ToList();
            var users = await DbContext.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

            foreach (var targetUser in users)
            {
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Bạn được chọn là người liên quan công việc mới",
                    Content = $"Bạn được gán là người liên quan trong công việc '{task.TenTaiLieu}'. Vui lòng xác nhận hoặc bình luận trong vòng 24 giờ.",
                    Link = $"/goi-thau/cong-viec/{task.Id}",
                    UserId = targetUser.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                DbContext.Notifications.Add(notification);
                await _hubContext.Clients.User(targetUser.Username).SendAsync("ReceiveNotification", notification);
            }
        }
        await DbContext.SaveChangesAsync();
    }
}
