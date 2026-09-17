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

    public DuAnService(
        AppDbContext dbContext,
        IMapper mapper,
        ICurrentUserService currentUserService,
        IDuAnSecurityService securityService,
        IDuAnNguonLinkService nguonLinkService,
        IDuAnBudgetService budgetService,
        IDuAnCascadeService cascadeService,
        IDuAnAuditService auditService,
        IDuAnNotificationService notificationService) : base(dbContext, mapper)
    {
        _currentUserService = currentUserService;
        _securityService = securityService;
        _nguonLinkService = nguonLinkService;
        _budgetService = budgetService;
        _cascadeService = cascadeService;
        _auditService = auditService;
        _notificationService = notificationService;
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
            .Include(da => da.DieuChinhs)
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

        if (filter.LoaiDuAn.HasValue)
        {
            query = query.Where(item => item.LoaiDuAn == filter.LoaiDuAn.Value);

            if (filter.LoaiDuAn.Value == 1 && !string.IsNullOrWhiteSpace(filter.Status))
            {
                var allowedSourceIds = new List<Guid>();
                if (filter.AllocatedProjectId.HasValue)
                {
                    var link = await DbContext.DuAnNguonTrienKhais
                        .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == filter.AllocatedProjectId.Value);
                    if (link?.NguonProjectId != null)
                    {
                        allowedSourceIds = link.NguonProjectId
                            .Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Select(Guid.Parse)
                            .ToList();
                    }
                }

                if (filter.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(da => da.TrangThai == (int)TrangThaiDuAn.Draft && da.DaKetThuc != true);
                }
                else if (filter.Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(da => (da.DaTrienKhai != true || allowedSourceIds.Contains(da.Id)) && da.TrangThai != (int)TrangThaiDuAn.Draft && da.DaKetThuc != true);
                }
                else if (filter.Status.Equals("Allocated", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(da => da.DaTrienKhai == true && da.DaKetThuc != true);
                }
                else if (filter.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(da => da.DaKetThuc == true);
                }
            }
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
            .Include(da => da.DieuChinhs)
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
            .Include(da => da.DieuChinhs)
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

            if (dto.LoaiDuAn == 2) // Du an trien khai
            {
                if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
                }

                if (dto.SourceProjectIds.Count != dto.SourceProjectIds.Distinct().Count())
                {
                    throw new InvalidOperationException("Danh sách dự án nguồn liên kết chứa mã dự án trùng lặp.");
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

                var linkedSourceIds = await _nguonLinkService.GetLinkedSourceProjectIdsAsync();
                var alreadyLinkedId = dto.SourceProjectIds.FirstOrDefault(id => linkedSourceIds.Contains(id));
                if (alreadyLinkedId != Guid.Empty)
                {
                    var conflictedProj = sourceProjects.FirstOrDefault(sp => sp.Id == alreadyLinkedId);
                    var projName = conflictedProj?.Name ?? alreadyLinkedId.ToString();
                    throw new InvalidOperationException($"Dự án nguồn '{projName}' đã thuộc về một dự án triển khai khác.");
                }

                var alreadyDeployedProj = sourceProjects.FirstOrDefault(da => da.DaTrienKhai == true);
                if (alreadyDeployedProj != null)
                {
                    throw new InvalidOperationException($"Dự án nguồn '{alreadyDeployedProj.Name}' đã thuộc về một dự án triển khai khác.");
                }

                foreach (var sp in sourceProjects)
                {
                    sp.DaTrienKhai = true;
                    DbSet.Update(sp);
                }

                entity.DaTrienKhai = true;

                entity.NguonDuAns.Add(new DuAnNguonTrienKhai
                {
                    TrienKhaiProjectId = entity.Id,
                    NguonProjectId = string.Join(";", dto.SourceProjectIds),
                    CreatedAt = DateTime.UtcNow
                });

                decimal totalAggregatedBudget = 0;
                foreach (var sp in sourceProjects)
                {
                    var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                    totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                }

                entity.DuToanPheDuyet = totalAggregatedBudget;

                if (!entity.NgayBatDau.HasValue)
                {
                    var sourceDates = sourceProjects
                        .Where(sp => sp.NgayBatDau.HasValue)
                        .Select(sp => sp.NgayBatDau!.Value)
                        .OrderBy(d => d)
                        .ToList();

                    if (sourceDates.Any())
                    {
                        entity.NgayBatDau = sourceDates.First();
                        if (!entity.NamBatDau.HasValue)
                        {
                            entity.NamBatDau = entity.NgayBatDau.Value.Year;
                        }
                    }
                }
            }
            else // Du an nguon
            {
                if (dto.SourceProjectIds != null && dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án nguồn không thể liên kết đến dự án nguồn khác.");
                }

                entity.LoaiDuAn = 1;
                entity.DaTrienKhai = false;
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

        foreach (var dto in dtoList.Where(d => d.LoaiDuAn == 2 && d.SourceProjectIds != null))
        {
            if (dto.SourceProjectIds!.Count != dto.SourceProjectIds!.Distinct().Count())
            {
                throw new InvalidOperationException("Danh sách dự án nguồn liên kết chứa mã dự án trùng lặp.");
            }
        }

        var allRawSourceProjectIds = dtoList
            .Where(d => d.LoaiDuAn == 2 && d.SourceProjectIds != null)
            .SelectMany(d => d.SourceProjectIds!)
            .ToList();

        var allSourceProjectIds = allRawSourceProjectIds
            .Distinct()
            .ToList();

        List<DuAn> sourceProjects = new List<DuAn>();
        if (allSourceProjectIds.Any())
        {
            sourceProjects = await DbSet.Include(da => da.DieuChinhs)
                .Where(da => allSourceProjectIds.Contains(da.Id))
                .ToListAsync();

            if (sourceProjects.Count != allSourceProjectIds.Count)
            {
                throw new ArgumentException("Một số dự án nguồn được chọn không tồn tại.");
            }

            if (sourceProjects.Any(da => da.LoaiDuAn != 1))
            {
                throw new ArgumentException("Chỉ được liên kết đến các dự án nguồn (loại dự án nguồn).");
            }

            var duplicateAcrossBatch = allRawSourceProjectIds
                .GroupBy(x => x)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateAcrossBatch != null)
            {
                var duplicateProj = sourceProjects.FirstOrDefault(sp => sp.Id == duplicateAcrossBatch.Key);
                var projName = duplicateProj?.Name ?? duplicateAcrossBatch.Key.ToString();
                throw new InvalidOperationException($"Dự án nguồn '{projName}' được liên kết nhiều hơn một lần trong danh sách tạo.");
            }

            var linkedSourceIds = await _nguonLinkService.GetLinkedSourceProjectIdsAsync();
            var alreadyLinkedId = allSourceProjectIds.FirstOrDefault(id => linkedSourceIds.Contains(id));
            if (alreadyLinkedId != Guid.Empty)
            {
                var conflictedProj = sourceProjects.FirstOrDefault(sp => sp.Id == alreadyLinkedId);
                var projName = conflictedProj?.Name ?? alreadyLinkedId.ToString();
                throw new InvalidOperationException($"Dự án nguồn '{projName}' đã thuộc về một dự án triển khai khác.");
            }

            var alreadyDeployedProj = sourceProjects.FirstOrDefault(da => da.DaTrienKhai == true);
            if (alreadyDeployedProj != null)
            {
                throw new InvalidOperationException($"Dự án nguồn '{alreadyDeployedProj.Name}' đã thuộc về một dự án triển khai khác.");
            }

            foreach (var sp in sourceProjects)
            {
                sp.DaTrienKhai = true;
                DbSet.Update(sp);
            }
        }

        var entities = new List<DuAn>();
        var now = DateTime.UtcNow;

        var currentUsername = _currentUserService.GetUsername();
        var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);

        foreach (var dto in dtoList)
        {
            var entity = Mapper.Map<DuAn>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = now;

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

            if (dto.LoaiDuAn == 2)
            {
                if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
                }

                entity.DaTrienKhai = true;
                entity.NguonDuAns.Add(new DuAnNguonTrienKhai
                {
                    TrienKhaiProjectId = entity.Id,
                    NguonProjectId = string.Join(";", dto.SourceProjectIds),
                    CreatedAt = now
                });

                var projectSources = sourceProjects.Where(sp => dto.SourceProjectIds.Contains(sp.Id)).ToList();
                decimal totalAggregatedBudget = 0;
                foreach (var sp in projectSources)
                {
                    var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                    totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                }
                entity.DuToanPheDuyet = totalAggregatedBudget;
            }
            else
            {
                if (dto.SourceProjectIds != null && dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án nguồn không thể liên kết đến dự án nguồn khác.");
                }

                entity.LoaiDuAn = 1;
                entity.DaTrienKhai = false;
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

            entities.Add(entity);
        }

        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();

        var createdIds = entities.Select(e => e.Id).ToList();
        var reloadedEntities = await DbSet.AsNoTracking()
            .Include(da => da.DieuChinhs)
            .Include(da => da.PhanKyVons)
            .Include(da => da.DanhSachNguonVon).ThenInclude(nv => nv.NguonVon)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Include(da => da.ChuDuAn)
            .Where(da => createdIds.Contains(da.Id))
            .ToListAsync();
        var resultDtos = Mapper.Map<List<DuAnDto>>(reloadedEntities);
        await _nguonLinkService.PopulateSourceProjectsAsync(resultDtos);
        return resultDtos;
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateDuAnDto dto)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            var entity = await DbSet
                .Include(da => da.DieuChinhs)
                .Include(da => da.PhanKyVons)
                .FirstOrDefaultAsync(da => da.Id == id);
            if (entity is null)
            {
                return false;
            }

            if (entity.LoaiDuAn == 2)
            {
                if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
                }

                if (dto.SourceProjectIds.Count != dto.SourceProjectIds.Distinct().Count())
                {
                    throw new InvalidOperationException("Danh sách dự án nguồn liên kết chứa mã dự án trùng lặp.");
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

                var otherLinkedSourceIds = await _nguonLinkService.GetLinkedSourceProjectIdsAsync(excludeTrienKhaiProjectId: id);
                var alreadyLinkedId = dto.SourceProjectIds.FirstOrDefault(spId => otherLinkedSourceIds.Contains(spId));
                if (alreadyLinkedId != Guid.Empty)
                {
                    var conflictedProj = sourceProjects.FirstOrDefault(sp => sp.Id == alreadyLinkedId);
                    var projName = conflictedProj?.Name ?? alreadyLinkedId.ToString();
                    throw new InvalidOperationException($"Dự án nguồn '{projName}' đã thuộc về một dự án triển khai khác.");
                }

                var currentLink = await DbContext.DuAnNguonTrienKhais
                    .FirstOrDefaultAsync(nk => nk.TrienKhaiProjectId == id);

                var oldSourceIds = currentLink?.NguonProjectId?
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Guid.Parse)
                    .ToList() ?? new List<Guid>();

                var addedIds = dto.SourceProjectIds.Except(oldSourceIds).ToList();
                var removedIds = oldSourceIds.Except(dto.SourceProjectIds).ToList();

                var newlyLinkedAlreadyDeployed = sourceProjects
                    .Where(da => addedIds.Contains(da.Id) && da.DaTrienKhai == true)
                    .ToList();

                if (newlyLinkedAlreadyDeployed.Any())
                {
                    var deployedProj = newlyLinkedAlreadyDeployed.First();
                    throw new InvalidOperationException($"Dự án nguồn '{deployedProj.Name}' đã thuộc về một dự án triển khai khác.");
                }

                foreach (var sp in sourceProjects.Where(da => addedIds.Contains(da.Id)))
                {
                    sp.DaTrienKhai = true;
                    DbSet.Update(sp);
                }

                if (removedIds.Any())
                {
                    var removedProjects = await DbSet.Where(da => removedIds.Contains(da.Id)).ToListAsync();
                    foreach (var rp in removedProjects)
                    {
                        rp.DaTrienKhai = false;
                        DbSet.Update(rp);
                    }
                }

                string newNguonProjectIdString = string.Join(";", dto.SourceProjectIds);
                if (currentLink != null)
                {
                    currentLink.NguonProjectId = newNguonProjectIdString;
                    DbContext.DuAnNguonTrienKhais.Update(currentLink);
                }
                else
                {
                    DbContext.DuAnNguonTrienKhais.Add(new DuAnNguonTrienKhai
                    {
                        TrienKhaiProjectId = id,
                        NguonProjectId = newNguonProjectIdString,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                entity.DaTrienKhai = true;

                decimal totalAggregatedBudget = 0;
                foreach (var sp in sourceProjects)
                {
                    var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                    totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                }

                entity.DuToanPheDuyet = totalAggregatedBudget;
                dto.DuToanPheDuyet = totalAggregatedBudget;

                var goiThauBudgetsSum = await DbContext.GoiThaus
                    .Where(gt => gt.DuAnId == id)
                    .SumAsync(gt => gt.GiaTriGoiThau);
                if (totalAggregatedBudget < goiThauBudgetsSum)
                {
                    throw new InvalidOperationException($"Tổng ngân sách dự án nguồn mới ({totalAggregatedBudget:N0} VNĐ) không đủ bao phủ tổng giá trị dự toán các gói thầu đã lập ({goiThauBudgetsSum:N0} VNĐ).");
                }
            }
            else
            {
                if (dto.SourceProjectIds != null && dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án nguồn không thể liên kết đến dự án nguồn khác.");
                }
            }

            DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc, dto.NgayKetThucThucTe);

            if (dto.DuToanPheDuyet != entity.DuToanPheDuyet)
            {
                if (entity.LoaiDuAn == 1)
                {
                    bool isStatusChanged = dto.TrangThai != entity.TrangThai;
                    bool isNotDeployedYet = entity.DaTrienKhai != true;

                    if (!isStatusChanged && !isNotDeployedYet)

                    {
                        throw new InvalidOperationException("Dự án nguồn đã triển khai không thể sửa đổi dự toán phê duyệt trực tiếp nếu không chuyển trạng thái. Vui lòng sử dụng chức năng điều chỉnh dự án.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Dự án triển khai không thể sửa đổi dự toán trực tiếp vì nó được tổng hợp tự động từ các dự án nguồn.");
                }
            }

            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
            if (currentUser != null && currentUser.IsSystemAdmin && dto.ChuDuAnId.HasValue)
            {
                entity.ChuDuAnId = dto.ChuDuAnId;
            }

            Mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            if (dto.PhanKyVons != null)
            {
                var duplicateYears = dto.PhanKyVons.GroupBy(x => x.Nam).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (duplicateYears.Any())
                {
                    throw new InvalidOperationException($"Phân kỳ vốn không được trùng lặp năm: {string.Join(", ", duplicateYears)}.");
                }

                var existingPhanKys = await DbContext.DuAnPhanKyVons.Where(p => p.DuAnId == id).ToListAsync();
                var updatedYears = dto.PhanKyVons.Select(x => x.Nam).ToList();
                var toRemove = existingPhanKys.Where(x => !updatedYears.Contains(x.Nam)).ToList();
                if (toRemove.Any())
                {
                    DbContext.DuAnPhanKyVons.RemoveRange(toRemove);
                }

                foreach (var pkDto in dto.PhanKyVons)
                {
                    var existing = existingPhanKys.FirstOrDefault(x => x.Nam == pkDto.Nam);
                    var percent = pkDto.TyLePercent;
                    if (!percent.HasValue || percent == 0)
                    {
                        percent = entity.DuToanPheDuyet > 0 ? Math.Round((pkDto.SoTienPhanKy / entity.DuToanPheDuyet) * 100, 2) : 0;
                    }

                    if (existing != null && !toRemove.Contains(existing))
                    {
                        existing.SoTienPhanKy = pkDto.SoTienPhanKy;
                        existing.TyLePercent = percent;
                        existing.GhiChu = pkDto.GhiChu;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        await DbContext.DuAnPhanKyVons.AddAsync(new DuAnPhanKyVon
                        {
                            Id = Guid.NewGuid(),
                            DuAnId = entity.Id,
                            Nam = pkDto.Nam,
                            SoTienPhanKy = pkDto.SoTienPhanKy,
                            TyLePercent = percent,
                            GhiChu = pkDto.GhiChu,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            if (dto.DanhSachNguonVon != null)
            {
                var existingNguonVons = await DbContext.DuAnNguonVons.Where(p => p.DuAnId == id).ToListAsync();
                var updatedNguonVonIds = dto.DanhSachNguonVon.Select(x => x.NguonVonId).ToList();
                var toRemoveNv = existingNguonVons.Where(x => !updatedNguonVonIds.Contains(x.NguonVonId)).ToList();
                if (toRemoveNv.Any())
                {
                    DbContext.DuAnNguonVons.RemoveRange(toRemoveNv);
                }

                foreach (var nvDto in dto.DanhSachNguonVon)
                {
                    var existingNv = existingNguonVons.FirstOrDefault(x => x.NguonVonId == nvDto.NguonVonId);
                    if (existingNv != null && !toRemoveNv.Contains(existingNv))
                    {
                        existingNv.SoTien = nvDto.SoTien;
                        existingNv.GhiChu = nvDto.GhiChu;
                        existingNv.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        await DbContext.DuAnNguonVons.AddAsync(new DuAnNguonVon
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
            }

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public Task<DieuChinhDuAnDto> AdjustBudgetAsync(Guid id, CreateDieuChinhDuAnDto dto)
    {
        return _budgetService.AdjustBudgetAsync(id, dto);
    }

    public Task<IReadOnlyList<DieuChinhDuAnDto>> GetAdjustmentsAsync(Guid id)
    {
        return _budgetService.GetAdjustmentsAsync(id);
    }

    public async Task<DuAnDto> AdvanceStatusAsync(Guid id)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "EDIT");

        if (entity.TrangThai >= (int)TrangThaiDuAn.HoanThanh)
        {
            throw new InvalidOperationException("Dự án đã ở trạng thái hoàn thành, không thể chuyển tiếp.");
        }

        entity.TrangThai = (int)TrangThaiDuAn.HoanThanh;
        entity.DaKetThuc = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<DuAnDto> CloseProjectAsync(Guid id)
    {
        var entity = await DbSet.Include(da => da.DieuChinhs).FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "EDIT");

        entity.TrangThai = (int)TrangThaiDuAn.HoanThanh;
        entity.DaKetThuc = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await DbContext.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public Task<IReadOnlyList<DuAnNguonSummaryDto>> GetSourceProjectsByProjectIdAsync(Guid id)
    {
        return _nguonLinkService.GetSourceProjectsByProjectIdAsync(id);
    }

    public async Task<IReadOnlyList<GoiThauDto>> GetGoiThausByProjectIdAsync(Guid id)
    {
        var entity = await DbSet.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");

        var items = await DbContext.GoiThaus
                                   .Where(gt => gt.DuAnId == id)
                                   .ToListAsync();
        return Mapper.Map<List<GoiThauDto>>(items);
    }

    public async Task<IReadOnlyList<HopDongDto>> GetHopDongsByProjectIdAsync(Guid id)
    {
        var entity = await DbSet.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            throw new KeyNotFoundException("Không tìm thấy dự án.");
        }

        await _securityService.EnsureUserHasProjectAccessAsync(entity, "VIEW");

        var items = await DbContext.HopDongs
                                   .Include(hd => hd.GoiThau)
                                   .Where(hd => hd.GoiThau != null && hd.GoiThau.DuAnId == id)
                                   .ToListAsync();
        return Mapper.Map<List<HopDongDto>>(items);
    }

    public Task<IReadOnlyList<AuditLog>> GetAuditLogsByProjectIdAsync(Guid id)
    {
        return _auditService.GetAuditLogsByProjectIdAsync(id);
    }

    public Task<bool> ChangeOwnerAsync(Guid projectId, Guid newOwnerId)
    {
        return _notificationService.ChangeOwnerAsync(projectId, newOwnerId);
    }

    public override Task<bool> DeleteAsync(Guid id)
    {
        return _cascadeService.DeleteAsync(id);
    }

    public override Task<bool> SoftDeleteAsync(Guid id)
    {
        return _cascadeService.SoftDeleteAsync(id);
    }

    public override Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        return _cascadeService.SoftDeleteAsync(ids);
    }

    public override Task<bool> RestoreAsync(Guid id)
    {
        return _cascadeService.RestoreAsync(id);
    }

    public override Task<bool> RestoreAsync(IEnumerable<Guid> ids)
    {
        return _cascadeService.RestoreAsync(ids);
    }

    public async Task<IReadOnlyList<DuAnLookupDto>> GetLookupAsync(int? loaiDuAn = null)
    {
        var query = DbSet.AsNoTracking()
            .Where(d => d.IsActive && !d.IsDeleted);

        if (loaiDuAn.HasValue)
        {
            query = query.Where(d => d.LoaiDuAn == loaiDuAn.Value);
        }

        return await query
            .OrderBy(d => d.Code)
            .ThenBy(d => d.Name)
            .Select(d => new DuAnLookupDto
            {
                Id = d.Id,
                Code = d.Code,
                Name = d.Name
            })
            .ToListAsync();
    }
}
