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

public class GoiThauService : DbCrudService<GoiThau, GoiThauDto, CreateGoiThauDto, UpdateGoiThauDto>, IGoiThauService
{
    private readonly ILogger<GoiThauService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public GoiThauService(AppDbContext dbContext, IMapper mapper, ILogger<GoiThauService> logger, ICurrentUserService currentUserService) : base(dbContext, mapper)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public override Task<PagedResult<GoiThauDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        return GetAllAsync(new GoiThauFilterDto
        {
            Search = search,
            Page = page,
            PageSize = pageSize,
            Cursor = cursor
        });
    }

    public async Task<PagedResult<GoiThauDto>> GetAllAsync(GoiThauFilterDto filter)
    {
        try
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);

            IQueryable<GoiThau> query = DbSet.AsNoTracking()
                .Include(gt => gt.DuAn);

            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
            if (currentUser != null && !currentUser.IsSystemAdmin)
            {
                query = query.Where(gt => gt.DuAn.CreatedByUserId == currentUser.Id || DbContext.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == gt.DuAnId));
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var keyword = filter.Search.Trim();
                query = ApplySearchFilter(query, keyword);
            }

            if (filter.DuAnId.HasValue)
            {
                query = query.Where(item => item.DuAnId == filter.DuAnId.Value);
            }

            if (filter.MinGiaTri.HasValue)
            {
                query = query.Where(item => item.GiaTriGoiThau >= filter.MinGiaTri.Value);
            }

            if (filter.MaxGiaTri.HasValue)
            {
                query = query.Where(item => item.GiaTriGoiThau <= filter.MaxGiaTri.Value);
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(item => item.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(item => item.CreatedAt <= filter.ToDate.Value);
            }

            var totalItems = await query.CountAsync();

            List<GoiThau> items;
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

            var dtos = Mapper.Map<List<GoiThauDto>>(items);
            await PopulateTongGiaTriHopDongAsync(dtos);

            return new PagedResult<GoiThauDto>
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
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<IReadOnlyList<GoiThauDto>> GetAllItemsAsync()
    {
        try
        {
            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
            IQueryable<GoiThau> query = DbSet.AsNoTracking().Include(gt => gt.DuAn);
            if (currentUser != null && !currentUser.IsSystemAdmin)
            {
                query = query.Where(gt => gt.DuAn.CreatedByUserId == currentUser.Id || DbContext.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == gt.DuAnId));
            }
            var items = await query.ToListAsync();
            var dtos = Mapper.Map<List<GoiThauDto>>(items);
            await PopulateTongGiaTriHopDongAsync(dtos);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetAllItemsAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<GoiThauDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await DbSet
                .Include(gt => gt.DuAn)
                .FirstOrDefaultAsync(gt => gt.Id == id);
            if (entity is null) return null;

            var dto = Mapper.Map<GoiThauDto>(entity);
            await PopulateTongGiaTriHopDongAsync(new List<GoiThauDto> { dto });
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetByIdAsync của GoiThauService cho ID {Id}.", id);
            throw;
        }
    }

    private async Task PopulateTongGiaTriHopDongAsync(List<GoiThauDto> dtos)
    {
        if (dtos == null || !dtos.Any()) return;

        var goiThauIds = dtos.Select(d => d.Id).ToList();

        var contractSums = await DbContext.HopDongs
            .Where(h => h.GoiThauId.HasValue && goiThauIds.Contains(h.GoiThauId.Value))
            .GroupBy(h => h.GoiThauId!.Value)
            .Select(g => new { GoiThauId = g.Key, Total = g.Sum(h => h.GiaTriHopDong) })
            .ToDictionaryAsync(x => x.GoiThauId, x => x.Total);

        foreach (var dto in dtos)
        {
            dto.TongGiaTriHopDong = contractSums.TryGetValue(dto.Id, out var sum) ? sum : 0;
        }
    }

    private async Task<GoiThau> CreateEntityInternalAsync(
        CreateGoiThauDto dto, 
        HashSet<string>? existingCodesInBatch = null,
        Dictionary<Guid, decimal>? projectBatchSum = null,
        User? preFetchedUser = null,
        Dictionary<Guid, DuAn>? preFetchedProjects = null,
        HashSet<Guid>? allowedProjectIds = null,
        HashSet<string>? existingCodesInDb = null)
    {
        GoiThauValidator.EnsureValid(dto.GiaTriGoiThau, dto.NguongCanhBaoPercent);

        if (dto.DuAnId.HasValue)
        {
            // Verify project budget limits
            DuAn? project = null;
            if (preFetchedProjects != null && preFetchedProjects.TryGetValue(dto.DuAnId.Value, out var cachedProject))
            {
                project = cachedProject;
            }
            else
            {
                project = await DbContext.DuAns.Include(da => da.DieuChinhs)
                                                 .Include(da => da.GoiThaus)
                                                 .FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
            }

            if (project == null)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }

            User? currentUser = null;
            if (preFetchedUser != null)
            {
                currentUser = preFetchedUser;
            }
            else
            {
                var currentUsername = _currentUserService.GetUsername();
                currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
            }

            if (currentUser == null)
            {
                throw new UnauthorizedAccessException("Bạn chưa đăng nhập.");
            }

            if (!currentUser.IsSystemAdmin && project.CreatedByUserId != currentUser.Id)
            {
                bool hasCreatePerm = false;
                if (allowedProjectIds != null)
                {
                    hasCreatePerm = allowedProjectIds.Contains(project.Id);
                }
                else
                {
                    hasCreatePerm = await DbContext.UserPermissions.AnyAsync(up =>
                        up.UserId == currentUser.Id &&
                        up.DuAnId == project.Id &&
                        up.Permission != null && up.Permission.Code == "CREATE");
                }

                if (!hasCreatePerm)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền tạo gói thầu trong dự án này.");
                }
            }
            var projectBudget = project.DuToanPheDuyet + (project.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0);
            var existingPackagesSum = project.GoiThaus?.Sum(gt => gt.GiaTriGoiThau) ?? 0;

            decimal batchSumForProject = 0;
            if (projectBatchSum != null && projectBatchSum.TryGetValue(dto.DuAnId.Value, out var sum))
            {
                batchSumForProject = sum;
            }

            if (existingPackagesSum + batchSumForProject + dto.GiaTriGoiThau > projectBudget)
            {
                throw new InvalidOperationException($"Tổng giá trị các gói thầu ({existingPackagesSum + batchSumForProject + dto.GiaTriGoiThau:N0} VNĐ) vượt quá tổng mức đầu tư của dự án ({projectBudget:N0} VNĐ).");
            }

            if (projectBatchSum != null)
            {
                projectBatchSum[dto.DuAnId.Value] = batchSumForProject + dto.GiaTriGoiThau;
            }
        }

        var entity = Mapper.Map<GoiThau>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        var codeLower = entity.Code.ToLower();
        // Validate unique code in DB
        bool exists = false;
        if (existingCodesInDb != null)
        {
            exists = existingCodesInDb.Contains(codeLower);
        }
        else
        {
            exists = await DbSet.AnyAsync(item => item.Code.ToLower() == codeLower);
        }

        if (exists)
        {
            throw new InvalidOperationException($"Mã gói thầu '{entity.Code}' đã tồn tại.");
        }

        // Validate unique code in batch if provided
        if (existingCodesInBatch != null)
        {
            if (existingCodesInBatch.Contains(codeLower))
            {
                throw new InvalidOperationException($"Mã gói thầu '{entity.Code}' bị trùng lặp trong danh sách thêm mới.");
            }
            existingCodesInBatch.Add(codeLower);
        }

        await DbSet.AddAsync(entity);

        return entity;
    }

    public override async Task<GoiThauDto> CreateAsync(CreateGoiThauDto dto)
    {
        try
        {
            var entity = await CreateEntityInternalAsync(dto);
            await DbContext.SaveChangesAsync();

            // Reload to get relationship mappings
            var reloaded = await DbSet
                .Include(gt => gt.DuAn)
                .FirstOrDefaultAsync(gt => gt.Id == entity.Id);
            var resultDto = Mapper.Map<GoiThauDto>(reloaded);
            await PopulateTongGiaTriHopDongAsync(new List<GoiThauDto> { resultDto });
            return resultDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CreateAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<IEnumerable<GoiThauDto>> CreateRangeAsync(IEnumerable<CreateGoiThauDto> dtos)
    {
        try
        {
            var dtoList = dtos.ToList();
            if (!dtoList.Any())
            {
                return Enumerable.Empty<GoiThauDto>();
            }

            var projectIds = dtoList
                .Where(d => d.DuAnId.HasValue)
                .Select(d => d.DuAnId!.Value)
                .Distinct()
                .ToList();

            var codes = dtoList
                .Where(d => !string.IsNullOrWhiteSpace(d.Code))
                .Select(d => d.Code.Trim().ToLower())
                .Distinct()
                .ToList();

            // 1. Fetch current user once
            var currentUsername = _currentUserService.GetUsername();
            var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);

            // 2. Fetch all projects in one query
            var projectsDict = new Dictionary<Guid, DuAn>();
            if (projectIds.Any())
            {
                var projectList = await DbContext.DuAns
                    .Include(da => da.DieuChinhs)
                    .Include(da => da.GoiThaus)
                    .Where(da => projectIds.Contains(da.Id))
                    .ToListAsync();
                projectsDict = projectList.ToDictionary(da => da.Id, da => da);
            }

            // 3. Fetch user permissions for all projects in one query
            var allowedProjectIds = new HashSet<Guid>();
            if (currentUser != null && projectIds.Any())
            {
                if (currentUser.IsSystemAdmin)
                {
                    allowedProjectIds = new HashSet<Guid>(projectIds);
                }
                else
                {
                    var allowed = await DbContext.UserPermissions
                        .Where(up => up.UserId == currentUser.Id && up.DuAnId.HasValue && projectIds.Contains(up.DuAnId.Value) && up.Permission != null && up.Permission.Code == "CREATE")
                        .Select(up => up.DuAnId.Value)
                        .ToListAsync();
                    allowedProjectIds = new HashSet<Guid>(allowed);
                }
            }

            // 4. Fetch existing codes in DB for the codes in the batch
            var existingCodesInDb = new HashSet<string>();
            if (codes.Any())
            {
                var dbCodes = await DbSet
                    .Where(item => codes.Contains(item.Code.ToLower()))
                    .Select(item => item.Code.ToLower())
                    .ToListAsync();
                existingCodesInDb = new HashSet<string>(dbCodes);
            }

            var entities = new List<GoiThau>();
            var codesInBatch = new HashSet<string>();
            var projectBatchSum = new Dictionary<Guid, decimal>();

            foreach (var dto in dtoList)
            {
                var entity = await CreateEntityInternalAsync(
                    dto, 
                    codesInBatch, 
                    projectBatchSum, 
                    currentUser, 
                    projectsDict, 
                    allowedProjectIds, 
                    existingCodesInDb);
                entities.Add(entity);
            }

            await DbContext.SaveChangesAsync();

            // Bulk reload
            var createdIds = entities.Select(e => e.Id).ToList();
            var reloadedEntities = await DbSet
                .Include(gt => gt.DuAn)
                .Where(gt => createdIds.Contains(gt.Id))
                .ToListAsync();

            var result = Mapper.Map<List<GoiThauDto>>(reloadedEntities);
            await PopulateTongGiaTriHopDongAsync(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong CreateRangeAsync của GoiThauService.");
            throw;
        }
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateGoiThauDto dto)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity is null)
            {
                return false;
            }

            GoiThauValidator.EnsureValid(dto.GiaTriGoiThau, dto.NguongCanhBaoPercent);

            if (dto.DuAnId.HasValue)
            {
                var project = await DbContext.DuAns.Include(da => da.DieuChinhs)
                                                 .Include(da => da.GoiThaus)
                                                 .FirstOrDefaultAsync(da => da.Id == dto.DuAnId.Value);
                if (project == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
                }

                var projectBudget = project.DuToanPheDuyet + (project.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0);
                var existingPackagesSum = project.GoiThaus?.Where(gt => gt.Id != id).Sum(gt => gt.GiaTriGoiThau) ?? 0;

                if (existingPackagesSum + dto.GiaTriGoiThau > projectBudget)
                {
                    throw new InvalidOperationException($"Tổng giá trị các gói thầu ({existingPackagesSum + dto.GiaTriGoiThau:N0} VNĐ) vượt quá tổng mức đầu tư của dự án ({projectBudget:N0} VNĐ).");
                }
            }

            var contractsSum = await DbContext.HopDongs
                .Where(h => h.GoiThauId == id)
                .SumAsync(h => h.GiaTriHopDong);
            if (dto.GiaTriGoiThau < contractsSum)
            {
                throw new InvalidOperationException($"Giá trị dự toán mới của gói thầu ({dto.GiaTriGoiThau:N0} VNĐ) không thể nhỏ hơn tổng giá trị hợp đồng đã ký ({contractsSum:N0} VNĐ).");
            }

            Mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong UpdateAsync của GoiThauService cho ID {Id}.", id);
            throw;
        }
    }

    public async Task<GoiThauDetailWithTasksDto?> GetDetailWithTasksAsync(Guid id)
    {
        try
        {
            var goiThauDto = await GetByIdAsync(id);
            if (goiThauDto == null) return null;

            var tasks = await DbContext.CongViecGoiThaus.AsNoTracking()
                .Include(e => e.NguoiLienQuans)
                    .ThenInclude(n => n.User)
                .Include(e => e.CreateUser)
                .Include(e => e.ModifiedUser)
                .Where(e => e.GoiThauId == id)
                .OrderBy(e => e.Stt)
                .ThenBy(e => e.CreatedAt)
                .ToListAsync();

            var taskDtos = Mapper.Map<List<CongViecGoiThauDto>>(tasks);

            if (taskDtos.Any())
            {
                var taskIds = taskDtos.Select(d => d.Id).ToList();
                
                // Populate Attachments
                var validTaskEntityTypes = new[] { "CONG_VIEC_GOI_THAU", "CONG_VIEC", "GOI_THAU_CONG_VIEC", "GOI_THAU" };
                var attachments = await DbContext.FileAttachments
                    .AsNoTracking()
                    .Where(fa => validTaskEntityTypes.Contains(fa.EntityType) && taskIds.Contains(fa.EntityId) && fa.IsActive)
                    .ToListAsync();

                var attachmentGroup = attachments.GroupBy(fa => fa.EntityId)
                    .ToDictionary(g => g.Key, g => g.Select(fa => new FileAttachmentDto
                    {
                        Id = fa.Id,
                        FileName = fa.FileName,
                        FilePath = fa.FilePath,
                        ContentType = fa.ContentType,
                        FileSize = fa.FileSize,
                        CreatedAt = fa.CreatedAt
                    }).ToList());

                // Count Comments
                var commentCounts = await DbContext.CommentCongViecGoiThaus
                    .Where(c => taskIds.Contains(c.CongViecGoiThauId) && !c.IsDeleted)
                    .GroupBy(c => c.CongViecGoiThauId)
                    .Select(g => new { CongViecGoiThauId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CongViecGoiThauId, x => x.Count);

                foreach (var dto in taskDtos)
                {
                    if (attachmentGroup.TryGetValue(dto.Id, out var fileList))
                    {
                        dto.FileAttachments = fileList;
                    }
                    
                    dto.SoBinhLuan = commentCounts.TryGetValue(dto.Id, out var count) ? count : 0;
                }
            }

            return new GoiThauDetailWithTasksDto
            {
                Detail = goiThauDto,
                CongViecs = taskDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetDetailWithTasksAsync của GoiThauService cho ID {Id}.", id);
            throw;
        }
    }

    public override Task<bool> SoftDeleteAsync(Guid id)
    {
        return SoftDeleteAsync(new[] { id });
    }

    public override async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await DbSet.Where(gt => idList.Contains(gt.Id) && !gt.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var userId = _currentUserService.GetUserId();
        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedByUserId = userId;
            entity.UpdatedAt = now;
        }

        // Cascade Soft Delete cho Hợp đồng thuộc Gói thầu
        var hopDongs = await DbContext.HopDongs
            .Where(hd => hd.GoiThauId.HasValue && idList.Contains(hd.GoiThauId.Value))
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();
        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = true;
            hd.DeletedAt = now;
            hd.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Công việc thuộc Gói thầu
        var congViecs = await DbContext.CongViecGoiThaus
            .Where(cv => idList.Contains(cv.GoiThauId))
            .ToListAsync();
        var congViecIds = congViecs.Select(cv => cv.Id).ToList();
        foreach (var cv in congViecs)
        {
            cv.IsDeleted = true;
            cv.DeletedAt = now;
            cv.DeletedByUserId = userId;
        }

        if (congViecIds.Any())
        {
            var comments = await DbContext.CommentCongViecGoiThaus
                .Where(c => congViecIds.Contains(c.CongViecGoiThauId))
                .ToListAsync();
            foreach (var c in comments)
            {
                c.IsDeleted = true;
                c.DeletedAt = now;
                c.DeletedByUserId = userId;
            }

            var nlqs = await DbContext.CongViecNguoiLienQuans
                .Where(nlq => congViecIds.Contains(nlq.CongViecGoiThauId))
                .ToListAsync();
            foreach (var nlq in nlqs)
            {
                nlq.IsDeleted = true;
                nlq.DeletedAt = now;
                nlq.DeletedByUserId = userId;
            }

            var lss = await DbContext.CongViecLichSuChuyenTieps
                .Where(ls => congViecIds.Contains(ls.CongViecGoiThauId))
                .ToListAsync();
            foreach (var ls in lss)
            {
                ls.IsDeleted = true;
                ls.DeletedAt = now;
                ls.DeletedByUserId = userId;
            }
        }

        // Cascade Soft Delete cho License / Bản quyền thuộc Gói thầu
        var licenses = await DbContext.Licenses
            .Where(l => (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value)))
            .ToListAsync();
        foreach (var l in licenses)
        {
            l.IsDeleted = true;
            l.DeletedAt = now;
            l.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Hàng hóa dịch vụ thuộc Hợp đồng của Gói thầu
        if (hopDongIds.Any())
        {
            var hangHoas = await DbContext.HangHoaDichVus
                .Where(h => hopDongIds.Contains(h.IdParent))
                .ToListAsync();
            foreach (var h in hangHoas)
            {
                h.IsDeleted = true;
                h.DeletedAt = now;
                h.DeletedByUserId = userId;
            }
        }

        await DbContext.SaveChangesAsync();
        return true;
    }

    public override Task<bool> RestoreAsync(Guid id)
    {
        return RestoreAsync(new[] { id });
    }

    public override async Task<bool> RestoreAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await DbSet.IgnoreQueryFilters().Where(e => idList.Contains(e.Id) && e.IsDeleted).ToListAsync();
        if (!entities.Any()) return false;

        var now = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;
            entity.UpdatedAt = now;
        }

        var hopDongs = await DbContext.HopDongs.IgnoreQueryFilters()
            .Where(hd => hd.GoiThauId.HasValue && idList.Contains(hd.GoiThauId.Value) && hd.IsDeleted)
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();
        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = false;
            hd.DeletedAt = null;
            hd.DeletedByUserId = null;
        }

        var congViecs = await DbContext.CongViecGoiThaus.IgnoreQueryFilters()
            .Where(cv => idList.Contains(cv.GoiThauId) && cv.IsDeleted)
            .ToListAsync();
        var congViecIds = congViecs.Select(cv => cv.Id).ToList();
        foreach (var cv in congViecs)
        {
            cv.IsDeleted = false;
            cv.DeletedAt = null;
            cv.DeletedByUserId = null;
        }

        if (congViecIds.Any())
        {
            var comments = await DbContext.CommentCongViecGoiThaus.IgnoreQueryFilters()
                .Where(c => congViecIds.Contains(c.CongViecGoiThauId) && c.IsDeleted)
                .ToListAsync();
            foreach (var c in comments)
            {
                c.IsDeleted = false;
                c.DeletedAt = null;
                c.DeletedByUserId = null;
            }

            var nlqs = await DbContext.CongViecNguoiLienQuans.IgnoreQueryFilters()
                .Where(nlq => congViecIds.Contains(nlq.CongViecGoiThauId) && nlq.IsDeleted)
                .ToListAsync();
            foreach (var nlq in nlqs)
            {
                nlq.IsDeleted = false;
                nlq.DeletedAt = null;
                nlq.DeletedByUserId = null;
            }

            var lss = await DbContext.CongViecLichSuChuyenTieps.IgnoreQueryFilters()
                .Where(ls => congViecIds.Contains(ls.CongViecGoiThauId) && ls.IsDeleted)
                .ToListAsync();
            foreach (var ls in lss)
            {
                ls.IsDeleted = false;
                ls.DeletedAt = null;
                ls.DeletedByUserId = null;
            }
        }

        var licenses = await DbContext.Licenses.IgnoreQueryFilters()
            .Where(l => (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value)) && l.IsDeleted)
            .ToListAsync();
        foreach (var l in licenses)
        {
            l.IsDeleted = false;
            l.DeletedAt = null;
            l.DeletedByUserId = null;
        }

        if (hopDongIds.Any())
        {
            var hangHoas = await DbContext.HangHoaDichVus.IgnoreQueryFilters()
                .Where(h => hopDongIds.Contains(h.IdParent) && h.IsDeleted)
                .ToListAsync();
            foreach (var h in hangHoas)
            {
                h.IsDeleted = false;
                h.DeletedAt = null;
                h.DeletedByUserId = null;
            }
        }

        await DbContext.SaveChangesAsync();
        return true;
    }
}
