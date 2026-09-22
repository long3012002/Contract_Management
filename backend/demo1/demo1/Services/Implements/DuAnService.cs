using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Hubs;
using demo1.Services.Implements.SubServices;
using demo1.Services.Interfaces;
using demo1.Services.Interfaces.SubServices;
using demo1.Validator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace demo1.Services.Implements;

public class DuAnService : DbCrudService<DuAn, DuAnDto, CreateDuAnDto, UpdateDuAnDto>, IDuAnService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDuAnSecurityService _securityService;
    private readonly IDuAnNguonLinkService _nguonLinkService;
    private readonly IDuAnBudgetService _budgetService;
    private readonly IDuAnCascadeService _cascadeService;
    private readonly IDuAnAuditService _auditService;
    private readonly IDuAnNotificationService _notificationService;
    private readonly ICodeGeneratorService _codeGeneratorService;

    public DuAnService(
        AppDbContext dbContext,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IDuAnSecurityService securityService,
        IDuAnNguonLinkService nguonLinkService,
        IDuAnBudgetService budgetService,
        IDuAnCascadeService cascadeService,
        IDuAnAuditService auditService,
        IDuAnNotificationService notificationService,
        ICodeGeneratorService codeGeneratorService) : base(dbContext, mapper)
    {
        _currentUserService = currentUserService;
        _securityService = securityService;
        _nguonLinkService = nguonLinkService;
        _budgetService = budgetService;
        _cascadeService = cascadeService;
        _auditService = auditService;
        _notificationService = notificationService;
        _codeGeneratorService = codeGeneratorService;
    }

    public override Task<PagedResult<DuAnDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        return GetAllAsync(new DuAnFilterDto
        {
            Search = search,
            Page = page,
            PageSize = pageSize,
            Cursor = cursor
        });
    }

    public async Task<PagedResult<DuAnDto>> GetAllAsync(DuAnFilterDto filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 1000);

        IQueryable<DuAn> query = DbSet.AsNoTracking()
            .Include(da => da.PhanKyVons)
            .Include(da => da.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Include(da => da.ChuDuAn);

        query = await _securityService.ApplyUserAccessFilterAsync(query);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        if (filter.TrangThai.HasValue && filter.TrangThai.Value > 0)
        {
            query = query.Where(item => item.TrangThai == filter.TrangThai.Value);
        }

        if (filter.Nam.HasValue && filter.Nam.Value > 0)
        {
            query = query.Where(item => item.NamBatDau == filter.Nam.Value || (item.NgayBatDau.HasValue && item.NgayBatDau.Value.Year == filter.Nam.Value));
        }

        if (filter.PhanLoaiDuAnId.HasValue && filter.PhanLoaiDuAnId.Value != Guid.Empty)
        {
            query = query.Where(item => item.PhanLoaiDuAnId == filter.PhanLoaiDuAnId.Value);
        }

        if (filter.StartDate.HasValue)
        {
            var start = filter.StartDate.Value.Date;
            query = query.Where(item => item.NgayBatDau.HasValue && item.NgayBatDau.Value.Date >= start);
        }

        if (filter.EndDate.HasValue)
        {
            var end = filter.EndDate.Value.Date;
            query = query.Where(item =>
                (item.NgayKetThucThucTe.HasValue && item.NgayKetThucThucTe.Value.Date <= end) ||
                (!item.NgayKetThucThucTe.HasValue && item.NgayKetThuc.HasValue && item.NgayKetThuc.Value.Date <= end));
        }

        var totalItems = await query.CountAsync();

        List<DuAn> items;
        bool isKeyset = TryParseCursor(filter.Cursor, out var lastCreatedAt, out var lastId);

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
        await _nguonLinkService.PopulateSourceProjectsAsync(dtos);

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
        IQueryable<DuAn> query = DbSet.AsNoTracking()
            .Include(da => da.PhanKyVons)
            .Include(da => da.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Include(da => da.ChuDuAn);

        query = await _securityService.ApplyUserAccessFilterAsync(query);

        var items = await query.ToListAsync();
        var dtos = Mapper.Map<List<DuAnDto>>(items);
        await _nguonLinkService.PopulateSourceProjectsAsync(dtos);
        return dtos;
    }

    public override async Task<DuAnDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(da => da.PhanKyVons)
            .Include(da => da.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Include(da => da.ChuDuAn)
            .FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null) return null;

        var dto = Mapper.Map<DuAnDto>(entity);
        await _nguonLinkService.PopulateSourceProjectsAsync(new List<DuAnDto> { dto });
        return dto;
    }

    public override async Task<DuAnDto> CreateAsync(CreateDuAnDto dto)
    {
        Guid createdId;
        using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Code))
                {
                    // Tạm comment code tự sinh mã để cho phép người dùng tự nhập:
                    // dto.Code = await _codeGeneratorService.GenerateDuAnCodeAsync();
                    throw new ArgumentException("Vui lòng nhập Mã dự án.");
                }
                else
                {
                    dto.Code = CodePrefixValidator.FormatDuAnCode(dto.Code);
                }
                DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc, dto.NgayKetThucThucTe);

                var entity = Mapper.Map<DuAn>(dto);
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;

                var currentUsername = _currentUserService.GetUsername();
                var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
                if (currentUser != null)
                {
                    entity.CreatedByUserId = currentUser.Id;
                    if (currentUser.IsSystemAdmin && dto.ChuDuAnId.HasValue)
                    {
                        entity.ChuDuAnId = dto.ChuDuAnId;
                    }
                    else
                    {
                        entity.ChuDuAnId = currentUser.Id;
                    }
                }

                var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == entity.Code.ToLower());
                if (exists)
                {
                    throw new InvalidOperationException($"Mã dự án '{entity.Code}' đã tồn tại.");
                }

                if (dto.PhanKyVons != null && dto.PhanKyVons.Any())
                {
                    var duplicateYears = dto.PhanKyVons.GroupBy(x => x.Nam).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (duplicateYears.Any())
                    {
                        throw new InvalidOperationException($"Phân kỳ vốn không được trùng lặp năm: {string.Join(", ", duplicateYears)}.");
                    }

                    foreach (var pkDto in dto.PhanKyVons)
                    {
                        var percent = pkDto.TyLePercent;
                        if (!percent.HasValue || percent == 0)
                        {
                            percent = entity.DuToanPheDuyet > 0 ? Math.Round((pkDto.SoTienPhanKy / entity.DuToanPheDuyet) * 100, 2) : 0;
                        }

                        entity.PhanKyVons.Add(new DuAnPhanKyVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = entity.Id,
                            Nam = pkDto.Nam,
                            SoTienPhanKy = pkDto.SoTienPhanKy,
                            TyLePercent = percent,
                            GhiChu = pkDto.GhiChu
                        });
                    }
                }

                if (dto.DanhSachNguonVon != null && dto.DanhSachNguonVon.Any())
                {
                    foreach (var nvDto in dto.DanhSachNguonVon)
                    {
                        entity.DanhSachNguonVon.Add(new DuAnNguonVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = entity.Id,
                            NguonVonId = nvDto.NguonVonId,
                            Nam = nvDto.Nam,
                            SoTien = nvDto.SoTien,
                            GhiChu = nvDto.GhiChu,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                if (entity.TrangThai <= 1 || entity.TrangThai == (int)TrangThaiDuAn.Draft)
                {
                    entity.TrangThai = (int)TrangThaiDuAn.Approved; // Mặc định Đã duyệt (3) khi thêm mới
                }

                await DbSet.AddAsync(entity);
                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                createdId = entity.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return (await GetByIdAsync(createdId))!;
    }

    public override async Task<IEnumerable<DuAnDto>> CreateRangeAsync(IEnumerable<CreateDuAnDto> dtos)
    {
        var dtoList = dtos.ToList();
        if (!dtoList.Any()) return Enumerable.Empty<DuAnDto>();

        foreach (var dto in dtoList)
        {
            dto.Code = CodePrefixValidator.FormatDuAnCode(dto.Code);
            DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc);
        }

        var incomingCodes = dtoList.Select(d => d.Code.Trim().ToLower()).Distinct().ToList();
        var existingCodes = await DbSet
            .Where(item => incomingCodes.Contains(item.Code.ToLower()))
            .Select(item => item.Code.ToLower())
            .ToListAsync();

        if (existingCodes.Any())
        {
            throw new InvalidOperationException($"Các mã dự án sau đã tồn tại: {string.Join(", ", existingCodes)}");
        }

        var results = new List<DuAnDto>();
        foreach (var dto in dtoList)
        {
            var created = await CreateAsync(dto);
            results.Add(created);
        }
        return results;
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateDuAnDto dto)
    {
        var entity = await DbSet
            .Include(d => d.PhanKyVons)
            .Include(d => d.DanhSachNguonVon)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (entity == null) return false;

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "UPDATE");

        var phanKyDtos = dto.PhanKyVons;
        var nguonVonDtos = dto.DanhSachNguonVon;

        dto.PhanKyVons = null;
        dto.DanhSachNguonVon = null;

        Mapper.Map(dto, entity);

        if (phanKyDtos != null)
        {
            decimal duToan = entity.DuToanPheDuyet;
            var incomingYears = phanKyDtos.Select(x => x.Nam).ToHashSet();

            // Delete items no longer present in DTO
            var toDelete = entity.PhanKyVons.Where(x => !incomingYears.Contains(x.Nam)).ToList();
            foreach (var item in toDelete)
            {
                DbContext.DuAnPhanKyVons.Remove(item);
                entity.PhanKyVons.Remove(item);
            }

            // Update existing or add new items
            foreach (var pkDto in phanKyDtos)
            {
                decimal percent = duToan > 0 ? Math.Round((pkDto.SoTienPhanKy / duToan) * 100, 2) : 0;
                var existing = entity.PhanKyVons.FirstOrDefault(x => x.Nam == pkDto.Nam);
                if (existing != null)
                {
                    existing.SoTienPhanKy = pkDto.SoTienPhanKy;
                    existing.TyLePercent = percent;
                    existing.GhiChu = pkDto.GhiChu;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newPk = new DuAnPhanKyVon
                    {
                        Id = Guid.NewGuid(),
                        DuAnId = entity.Id,
                        Nam = pkDto.Nam,
                        SoTienPhanKy = pkDto.SoTienPhanKy,
                        TyLePercent = percent,
                        GhiChu = pkDto.GhiChu,
                        CreatedAt = DateTime.UtcNow
                    };
                    DbContext.DuAnPhanKyVons.Add(newPk);
                }
            }
        }

        if (nguonVonDtos != null)
        {
            var incomingKeys = nguonVonDtos.Select(x => (x.NguonVonId, x.Nam)).ToHashSet();

            var toDelete = entity.DanhSachNguonVon.Where(x => !incomingKeys.Contains((x.NguonVonId, x.Nam))).ToList();
            foreach (var item in toDelete)
            {
                DbContext.DuAnNguonVons.Remove(item);
                entity.DanhSachNguonVon.Remove(item);
            }

            foreach (var nvDto in nguonVonDtos)
            {
                var existing = entity.DanhSachNguonVon.FirstOrDefault(x => x.NguonVonId == nvDto.NguonVonId && x.Nam == nvDto.Nam);
                if (existing != null)
                {
                    existing.SoTien = nvDto.SoTien;
                    existing.GhiChu = nvDto.GhiChu;
                }
                else
                {
                    var newNv = new DuAnNguonVon
                    {
                        Id = Guid.NewGuid(),
                        DuAnId = entity.Id,
                        NguonVonId = nvDto.NguonVonId,
                        Nam = nvDto.Nam,
                        SoTien = nvDto.SoTien,
                        GhiChu = nvDto.GhiChu,
                        CreatedAt = DateTime.UtcNow
                    };
                    DbContext.DuAnNguonVons.Add(newNv);
                }
            }
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<DuAnDto> AdvanceStatusAsync(Guid id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án với ID [{id}].");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "UPDATE");

        if (entity.TrangThai == (int)TrangThaiDuAn.Completed || entity.TrangThai == (int)TrangThaiDuAn.Merged)
        {
            throw new InvalidOperationException($"Dự án [{entity.Code}] đã ở trạng thái kết thúc, không thể chuyển tiếp.");
        }

        entity.TrangThai += 1;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<DuAnDto> CloseProjectAsync(Guid id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án với ID [{id}].");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "UPDATE");

        entity.TrangThai = (int)TrangThaiDuAn.Completed;
        entity.DaKetThuc = true;
        entity.NgayKetThucThucTe = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<DuAnDto> GopDuAnAsync(Guid sourceId, GopDuAnDto dto, Guid currentUserId)
    {
        if (sourceId == dto.TargetDuAnId)
        {
            throw new ArgumentException("Dự án nguồn và Dự án đích nhận gộp không được trùng nhau.");
        }

        var sourceProject = await DbSet.FirstOrDefaultAsync(d => d.Id == sourceId);
        if (sourceProject == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án nguồn với ID [{sourceId}].");
        }

        if (sourceProject.TrangThai == (int)TrangThaiDuAn.Merged)
        {
            throw new InvalidOperationException($"Dự án [{sourceProject.Code}] đã ở trạng thái Đã gộp (Merged), không thể thực hiện gộp tiếp.");
        }

        var targetProject = await DbSet.FirstOrDefaultAsync(d => d.Id == dto.TargetDuAnId);
        if (targetProject == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án đích nhận gộp với ID [{dto.TargetDuAnId}].");
        }

        if (targetProject.TrangThai == (int)TrangThaiDuAn.Merged)
        {
            throw new InvalidOperationException($"Dự án đích [{targetProject.Code}] đã ở trạng thái Đã gộp (Merged), không thể nhận gộp.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(sourceProject, "UPDATE");
        await _securityService.EnsureUserHasProjectAccessAsync(targetProject, "UPDATE");

        Guid targetId;
        using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            try
            {
                // 1. Tạo bản ghi tham chiếu liên kết gộp DuAnGopLink
                var gopLink = new DuAnGopLink
                {
                    Id = Guid.NewGuid(),
                    SourceDuAnId = sourceId,
                    TargetDuAnId = dto.TargetDuAnId,
                    NgayGop = DateTime.UtcNow,
                    NguoiThucHienId = currentUserId,
                    DuToanLucGop = sourceProject.DuToanPheDuyet,
                    GhiChu = dto.GhiChu ?? $"Gộp từ dự án {sourceProject.Code} sang dự án {targetProject.Code}"
                };

                DbContext.DuAnGopLinks.Add(gopLink);

                // 2. Cập nhật trạng thái dự án nguồn sang Merged (10)
                sourceProject.TrangThai = (int)TrangThaiDuAn.Merged;
                sourceProject.UpdatedAt = DateTime.UtcNow;

                // 3. Chuyển quyền sở hữu / tham chiếu tất cả Gói thầu, Hợp đồng, License sang dự án đích
                var goiThaus = await DbContext.GoiThaus.Where(g => g.DuAnId == sourceId).ToListAsync();
                foreach (var gt in goiThaus)
                {
                    gt.DuAnId = dto.TargetDuAnId;
                }

                var hopDongs = await DbContext.HopDongs.Where(h => h.DuAnId == sourceId).ToListAsync();
                foreach (var hd in hopDongs)
                {
                    hd.DuAnId = dto.TargetDuAnId;
                }

                var licenses = await DbContext.Licenses.Where(l => l.DuAnId == sourceId).ToListAsync();
                foreach (var lc in licenses)
                {
                    lc.DuAnId = dto.TargetDuAnId;
                }

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                targetId = targetProject.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return (await GetByIdAsync(targetId))!;
    }

    public async Task<DuAnDto> HuyGopDuAnAsync(Guid targetDuAnId, HuyGopDuAnDto dto, Guid currentUserId)
    {
        var targetProject = await DbSet.FirstOrDefaultAsync(d => d.Id == targetDuAnId);
        if (targetProject == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án đích với ID [{targetDuAnId}].");
        }

        // 1. Tìm bản ghi DuAnGopLink
        DuAnGopLink? gopLink = null;
        if (dto.GopLinkId.HasValue && dto.GopLinkId.Value != Guid.Empty)
        {
            gopLink = await DbContext.DuAnGopLinks.FirstOrDefaultAsync(l => l.Id == dto.GopLinkId.Value && l.TargetDuAnId == targetDuAnId);
        }
        else if (dto.SourceDuAnId.HasValue && dto.SourceDuAnId.Value != Guid.Empty)
        {
            gopLink = await DbContext.DuAnGopLinks.FirstOrDefaultAsync(l => l.TargetDuAnId == targetDuAnId && l.SourceDuAnId == dto.SourceDuAnId.Value);
        }

        if (gopLink == null)
        {
            throw new KeyNotFoundException("Không tìm thấy liên kết gộp dự án tương ứng.");
        }

        var sourceId = gopLink.SourceDuAnId;
        var sourceProject = await DbSet.FirstOrDefaultAsync(d => d.Id == sourceId);
        if (sourceProject == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Dự án nguồn với ID [{sourceId}].");
        }

        // 2. Quyền truy cập
        await _securityService.EnsureUserHasProjectAccessAsync(sourceProject, "UPDATE");
        await _securityService.EnsureUserHasProjectAccessAsync(targetProject, "UPDATE");

        // 3. Kiểm tra điều kiện "chưa phát sinh nghiệm thu/giải ngân tiếp theo"
        var targetHopDongIds = await DbContext.HopDongs
            .Where(h => h.DuAnId == targetDuAnId || h.DuAnId == sourceId)
            .Select(h => h.Id)
            .ToListAsync();

        if (targetHopDongIds.Any())
        {
            var hasNewPayments = await DbContext.DotThanhToans
                .AnyAsync(d => targetHopDongIds.Contains(d.HopDongId) &&
                               (d.IsPaid || d.NgayThanhToanThucTe != null || d.CreatedAt > gopLink.NgayGop));

            if (hasNewPayments)
            {
                throw new InvalidOperationException("Không thể hủy gộp dự án do đã phát sinh đợt nghiệm thu/thanh toán/giải ngân sau thời điểm gộp.");
            }
        }

        using (var transaction = await DbContext.Database.BeginTransactionAsync())
        {
            try
            {
                // 4. Trả trạng thái dự án nguồn từ Merged (10) về lại Draft (1 - Bản nháp)
                sourceProject.TrangThai = (int)TrangThaiDuAn.Draft;
                sourceProject.UpdatedAt = DateTime.UtcNow;

                // 5. Chuyển trả lại các Gói thầu, Hợp đồng, License về dự án nguồn
                var goiThaus = await DbContext.GoiThaus.Where(g => g.DuAnId == targetDuAnId).ToListAsync();
                foreach (var gt in goiThaus)
                {
                    gt.DuAnId = sourceId;
                }

                var hopDongs = await DbContext.HopDongs.Where(h => h.DuAnId == targetDuAnId).ToListAsync();
                foreach (var hd in hopDongs)
                {
                    hd.DuAnId = sourceId;
                }

                var licenses = await DbContext.Licenses.Where(l => l.DuAnId == targetDuAnId).ToListAsync();
                foreach (var lc in licenses)
                {
                    lc.DuAnId = sourceId;
                }

                // 6. Xóa bản ghi trong DuAnGopLink
                DbContext.DuAnGopLinks.Remove(gopLink);

                // 7. Ghi Audit Log cho cả 2 dự án
                var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
                var actorName = currentUser?.FullName ?? currentUser?.Username ?? "Hệ thống";

                var auditLogTarget = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId.ToString(),
                    Username = actorName,
                    Action = "UNMERGE_PROJECT",
                    TableName = "DuAn",
                    EntityId = targetProject.Id.ToString(),
                    Description = $"Hủy gộp dự án {sourceProject.Code} khỏi dự án {targetProject.Code}. Lý do: {dto.GhiChu ?? "Không có"}",
                    Timestamp = DateTime.UtcNow
                };

                var auditLogSource = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId.ToString(),
                    Username = actorName,
                    Action = "UNMERGE_PROJECT",
                    TableName = "DuAn",
                    EntityId = sourceProject.Id.ToString(),
                    Description = $"Được hủy gộp khỏi dự án {targetProject.Code}, hoàn trả trạng thái về Bản nháp. Lý do: {dto.GhiChu ?? "Không có"}",
                    Timestamp = DateTime.UtcNow
                };

                DbContext.AuditLogs.AddRange(auditLogTarget, auditLogSource);

                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return (await GetByIdAsync(targetProject.Id))!;
    }

    public async Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id)
    {
        var entities = await DbContext.GoiThaus
            .AsNoTracking()
            .Where(g => g.DuAnId == id)
            .ToListAsync();
        return Mapper.Map<List<GoiThauDto>>(entities);
    }

    public async Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id)
    {
        var entities = await DbContext.HopDongs
            .AsNoTracking()
            .Where(h => h.DuAnId == id)
            .ToListAsync();
        return Mapper.Map<List<HopDongDto>>(entities);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id)
    {
        return await _auditService.GetAuditLogsByProjectIdAsync(id);
    }

    public async Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId)
    {
        var duAn = await DbContext.DuAns.FindAsync(projectId);
        if (duAn == null) return false;
        duAn.CreatedByUserId = newOwnerId;
        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<DuAnLookupDto>> GetLookupAsync()
    {
        var items = await DbSet
            .AsNoTracking()
            .Where(da => da.TrangThai != (int)TrangThaiDuAn.Merged)
            .OrderBy(da => da.Code)
            .Select(da => new DuAnLookupDto
            {
                Id = da.Id,
                Code = da.Code,
                Name = da.Name
            })
            .ToListAsync();

        return items;
    }

    public override async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null) return false;

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "DELETE");
        return await _cascadeService.DeleteAsync(id);
    }

    public override async Task<bool> SoftDeleteAsync(Guid id)
    {
        return await SoftDeleteAsync(new[] { id });
    }

    public override async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList == null || !idList.Any()) return false;

        var entities = await DbSet.Where(d => idList.Contains(d.Id)).ToListAsync();
        if (!entities.Any()) return false;

        foreach (var entity in entities)
        {
            await _securityService.EnsureUserHasProjectAccessAsync(entity, "DELETE");
        }

        return await _cascadeService.SoftDeleteAsync(idList);
    }

    public override async Task<bool> RestoreAsync(Guid id)
    {
        return await RestoreAsync(new[] { id });
    }

    public override async Task<bool> RestoreAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList == null || !idList.Any()) return false;

        var entities = await DbSet.IgnoreQueryFilters().Where(d => idList.Contains(d.Id) && d.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        foreach (var entity in entities)
        {
            await _securityService.EnsureUserHasProjectAccessAsync(entity, "DELETE");
        }

        return await _cascadeService.RestoreAsync(idList);
    }

    private static IQueryable<DuAn> ApplySearchFilter(IQueryable<DuAn> query, string keyword)
    {
        return query.Where(item =>
            item.Code.ToLower().Contains(keyword.ToLower()) ||
            item.Name.ToLower().Contains(keyword.ToLower()) ||
            (item.ChuDauTu != null && item.ChuDauTu.ToLower().Contains(keyword.ToLower())) ||
            (item.NoiDung != null && item.NoiDung.ToLower().Contains(keyword.ToLower())));
    }

    private static bool TryParseCursor(string? cursor, out DateTime createdAt, out Guid id)
    {
        createdAt = DateTime.MinValue;
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(cursor)) return false;

        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = decoded.Split('|');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out createdAt) && Guid.TryParse(parts[1], out id))
            {
                return true;
            }
        }
        catch { }

        return false;
    }

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var raw = $"{createdAt:o}|{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }
}
