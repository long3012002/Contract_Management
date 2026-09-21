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
        using var transaction = await DbContext.Database.BeginTransactionAsync();
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
                        SoTien = nvDto.SoTien,
                        GhiChu = nvDto.GhiChu,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await DbSet.AddAsync(entity);
            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return (await GetByIdAsync(entity.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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

        using var transaction = await DbContext.Database.BeginTransactionAsync();
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

            return (await GetByIdAsync(targetProject.Id))!;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
