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
    private readonly ICurrentUserService _currentUserService;

    public DuAnService(AppDbContext dbContext, IMapper mapper, ICurrentUserService currentUserService) : base(dbContext, mapper)
    {
        _currentUserService = currentUserService;
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
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn);

        var currentUsername = _currentUserService.GetUsername();
        var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (currentUser != null && !currentUser.IsSystemAdmin)
        {
            query = query.Where(da => da.CreatedByUserId == currentUser.Id 
                || DbContext.UserPermissions.Any(up => up.UserId == currentUser.Id && up.DuAnId == da.Id)
                || DbContext.CongViecNguoiLienQuans.Any(nlq => nlq.UserId == currentUser.Id && nlq.CongViecGoiThau != null && nlq.CongViecGoiThau.GoiThau != null && nlq.CongViecGoiThau.GoiThau.DuAnId == da.Id));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        if (filter.LoaiDuAn.HasValue)
        {
            query = query.Where(item => item.LoaiDuAn == filter.LoaiDuAn.Value);
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
        var items = await DbSet
            .Include(da => da.DieuChinhs)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .ToListAsync();
        return Mapper.Map<List<DuAnDto>>(items);
    }

    public override async Task<DuAnDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(da => da.DieuChinhs)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null) return null;

        return Mapper.Map<DuAnDto>(entity);
    }

    public override async Task<DuAnDto> CreateAsync(CreateDuAnDto dto)
    {
        DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc);
        
        var entity = Mapper.Map<DuAn>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        var currentUsername = _currentUserService.GetUsername();
        var currentUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == currentUsername);
        if (currentUser != null)
        {
            entity.CreatedByUserId = currentUser.Id;
        }

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
            var alreadyDeployedProj = sourceProjects.FirstOrDefault(da => da.DaTrienKhai == true);
            if (alreadyDeployedProj != null)
            {
                throw new InvalidOperationException($"Dự án nguồn '{alreadyDeployedProj.Name}' đã thuộc về một dự án triển khai khác.");
            }

            // Mark source projects as deployed
            foreach (var sp in sourceProjects)
            {
                sp.DaTrienKhai = true;
                DbSet.Update(sp);
            }

            entity.DaTrienKhai = true;

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
            entity.DaTrienKhai = false;
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

    public override async Task<IEnumerable<DuAnDto>> CreateRangeAsync(IEnumerable<CreateDuAnDto> dtos)
    {
        var dtoList = dtos.ToList();
        if (!dtoList.Any()) return Enumerable.Empty<DuAnDto>();

        // 1. Xác thực các DTO locally
        foreach (var dto in dtoList)
        {
            DuAnValidator.EnsureValid(dto.DuToanPheDuyet, dto.NgayBatDau, dto.NgayKetThuc, dto.NamBatDau, dto.NamKetThuc);
        }

        // 2. Kiểm tra tính duy nhất của mã dự án theo lô
        var incomingCodes = dtoList.Select(d => d.Code.Trim().ToLower()).Distinct().ToList();
        var existingCodes = await DbSet
            .Where(item => incomingCodes.Contains(item.Code.ToLower()))
            .Select(item => item.Code.ToLower())
            .ToListAsync();

        if (existingCodes.Any())
        {
            throw new InvalidOperationException($"Các mã dự án sau đã tồn tại: {string.Join(", ", existingCodes)}");
        }

        // 3. Tải toàn bộ dự án nguồn liên kết trong 1 truy vấn SQL
        var allSourceProjectIds = dtoList
            .Where(d => d.LoaiDuAn == 2 && d.SourceProjectIds != null)
            .SelectMany(d => d.SourceProjectIds!)
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

            var alreadyDeployedProj = sourceProjects.FirstOrDefault(da => da.DaTrienKhai == true);
            if (alreadyDeployedProj != null)
            {
                throw new InvalidOperationException($"Dự án nguồn '{alreadyDeployedProj.Name}' đã thuộc về một dự án triển khai khác.");
            }

            // Đánh dấu các dự án nguồn đã triển khai
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
            }

            if (dto.LoaiDuAn == 2)
            {
                if (dto.SourceProjectIds == null || !dto.SourceProjectIds.Any())
                {
                    throw new ArgumentException("Dự án triển khai bắt buộc phải có ít nhất một dự án nguồn liên kết.");
                }

                entity.DaTrienKhai = true;
                entity.NguonDuAnIds = string.Join(";", dto.SourceProjectIds.Select(id => id.ToString()));

                // Tính toán ngân sách từ các dự án nguồn
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
                entity.LoaiDuAn = 1;
                entity.NguonDuAnIds = null;
                entity.DaTrienKhai = false;
            }

            entities.Add(entity);
        }

        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync(); // Chỉ gọi SaveChanges 1 lần duy nhất

        return Mapper.Map<List<DuAnDto>>(entities);
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

            // Parse existing source project IDs from current entity
            var oldSourceIds = entity.NguonDuAnIds?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                                   .Where(g => g != Guid.Empty)
                                                   .ToList() ?? new List<Guid>();

            // Check if any newly added source project is already deployed in another project
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

            // Mark newly added source projects as deployed
            foreach (var sp in sourceProjects.Where(da => addedIds.Contains(da.Id)))
            {
                sp.DaTrienKhai = true;
                DbSet.Update(sp);
            }

            // Mark removed source projects as not deployed
            if (removedIds.Any())
            {
                var removedProjects = await DbSet.Where(da => removedIds.Contains(da.Id)).ToListAsync();
                foreach (var rp in removedProjects)
                {
                    rp.DaTrienKhai = false;
                    DbSet.Update(rp);
                }
            }

            entity.DaTrienKhai = true;
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

            // Check if new budget is less than the sum of GoiThau's budgets of this implementation project
            var goiThauBudgetsSum = await DbContext.GoiThaus
                .Where(gt => gt.DuAnId == id)
                .SumAsync(gt => gt.GiaTriGoiThau);
            if (totalAggregatedBudget < goiThauBudgetsSum)
            {
                throw new InvalidOperationException($"Tổng ngân sách dự án nguồn mới ({totalAggregatedBudget:N0} VNĐ) không đủ bao phủ tổng giá trị dự toán các gói thầu đã lập ({goiThauBudgetsSum:N0} VNĐ).");
            }
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
        var targetIdString = id.ToString();
        var implementationProjects = await DbSet
            .Where(da => da.LoaiDuAn == 2 && da.NguonDuAnIds != null && EF.Functions.Like(da.NguonDuAnIds, $"%{targetIdString}%"))
            .ToListAsync();
        if (implementationProjects.Any())
        {
            var allSourceIds = implementationProjects
                .SelectMany(ip => ip.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                 .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty))
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();

            var sourceProjectsDict = new Dictionary<Guid, DuAn>();
            if (allSourceIds.Any())
            {
                var sourceProjectsList = await DbSet.Include(da => da.DieuChinhs)
                                                    .Where(da => allSourceIds.Contains(da.Id))
                                                    .ToListAsync();
                sourceProjectsDict = sourceProjectsList.ToDictionary(sp => sp.Id, sp => sp);
            }

            var implementationProjectIds = implementationProjects.Select(ip => ip.Id).ToList();
            var goiThauBudgetsDict = new Dictionary<Guid, decimal>();
            if (implementationProjectIds.Any())
            {
                goiThauBudgetsDict = await DbContext.GoiThaus
                    .Where(gt => gt.DuAnId.HasValue && implementationProjectIds.Contains(gt.DuAnId.Value))
                    .GroupBy(gt => gt.DuAnId!.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.Sum(gt => gt.GiaTriGoiThau));
            }

            foreach (var ip in implementationProjects)
            {
                var sourceIds = ip.NguonDuAnIds!.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                               .Where(g => g != Guid.Empty)
                                               .ToList();

                if (sourceIds.Contains(id))
                {
                    decimal totalAggregatedBudget = 0;
                    foreach (var spId in sourceIds)
                    {
                        if (sourceProjectsDict.TryGetValue(spId, out var sp))
                        {
                            var adjustmentsSum = sp.DieuChinhs?.Sum(dc => dc.GiaTriDieuChinh) ?? 0;
                            totalAggregatedBudget += (sp.DuToanPheDuyet + adjustmentsSum);
                        }
                    }

                    ip.DuToanPheDuyet = totalAggregatedBudget;

                    decimal goiThauBudgetsSum = 0;
                    if (goiThauBudgetsDict.TryGetValue(ip.Id, out var sum))
                    {
                        goiThauBudgetsSum = sum;
                    }

                    if (totalAggregatedBudget < goiThauBudgetsSum)
                    {
                        throw new InvalidOperationException($"Điều chỉnh ngân sách làm cho tổng ngân sách của dự án triển khai liên kết '{ip.Name}' ({totalAggregatedBudget:N0} VNĐ) không đủ bao phủ các gói thầu đã lập ({goiThauBudgetsSum:N0} VNĐ).");
                    }

                    ip.UpdatedAt = DateTime.UtcNow;
                }
            }
            await DbContext.SaveChangesAsync();
        }

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

    public async Task<IReadOnlyList<DuAnNguonSummaryDto>> GetSourceProjectsByProjectIdAsync(Guid id)
    {
        var entity = await DbSet.AsNoTracking().FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null || string.IsNullOrWhiteSpace(entity.NguonDuAnIds))
        {
            return new List<DuAnNguonSummaryDto>();
        }

        var sourceGuids = entity.NguonDuAnIds
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        if (!sourceGuids.Any())
        {
            return new List<DuAnNguonSummaryDto>();
        }

        var sourceEntities = await DbSet.AsNoTracking()
            .Include(da => da.DieuChinhs)
            .Include(da => da.NhomDuAn)
            .Include(da => da.PhanLoaiDuAn)
            .Where(da => sourceGuids.Contains(da.Id))
            .ToListAsync();

        return Mapper.Map<List<DuAnNguonSummaryDto>>(sourceEntities);
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

    public override async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await DbSet.FirstOrDefaultAsync(da => da.Id == id);
        if (entity is null)
        {
            return false;
        }

        // 1. Tìm và xoá tất cả hợp đồng liên quan tới dự án hoặc gói thầu thuộc dự án
        var hopDongs = await DbContext.HopDongs
            .Where(hd => hd.DuAnId == id || (hd.GoiThau != null && hd.GoiThau.DuAnId == id))
            .ToListAsync();
        if (hopDongs.Any())
        {
            DbContext.HopDongs.RemoveRange(hopDongs);
        }

        // 2. Tìm và xoá tất cả gói thầu thuộc dự án
        var goiThaus = await DbContext.GoiThaus
            .Where(gt => gt.DuAnId == id)
            .ToListAsync();
        if (goiThaus.Any())
        {
            DbContext.GoiThaus.RemoveRange(goiThaus);
        }

        if (entity.LoaiDuAn == 2 && !string.IsNullOrWhiteSpace(entity.NguonDuAnIds))
        {
            var sourceIds = entity.NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                                .Where(g => g != Guid.Empty)
                                                .ToList();
            if (sourceIds.Any())
            {
                var sourceProjects = await DbSet.Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                foreach (var sp in sourceProjects)
                {
                    sp.DaTrienKhai = false;
                }
            }
        }

        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync();
        return true;
    }

    public override Task<bool> SoftDeleteAsync(Guid id)
    {
        return SoftDeleteAsync(new[] { id });
    }

    public override async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids)
    {
        var idList = ids?.Where(i => i != Guid.Empty).Distinct().ToList();
        if (idList is null || !idList.Any()) return false;

        var entities = await DbSet.Where(da => idList.Contains(da.Id) && !da.IsDeleted).ToListAsync();
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

        // Cascade Soft Delete cho Gói thầu
        var goiThaus = await DbContext.GoiThaus
            .Where(gt => gt.DuAnId.HasValue && idList.Contains(gt.DuAnId.Value))
            .ToListAsync();
        var goiThauIds = goiThaus.Select(gt => gt.Id).ToList();

        foreach (var gt in goiThaus)
        {
            gt.IsDeleted = true;
            gt.DeletedAt = now;
            gt.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Hợp đồng
        var hopDongs = await DbContext.HopDongs
            .Where(hd => (hd.DuAnId.HasValue && idList.Contains(hd.DuAnId.Value)) || (hd.GoiThauId.HasValue && goiThauIds.Contains(hd.GoiThauId.Value)))
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();

        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = true;
            hd.DeletedAt = now;
            hd.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Công việc gói thầu
        var congViecs = await DbContext.CongViecGoiThaus
            .Where(cv => cv.GoiThau != null && cv.GoiThau.DuAnId.HasValue && idList.Contains(cv.GoiThau.DuAnId.Value))
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

        // Cascade Soft Delete cho Điều chỉnh dự án
        var dieuChinhs = await DbContext.DieuChinhDuAns
            .Where(dc => idList.Contains(dc.DuAnId))
            .ToListAsync();
        foreach (var dc in dieuChinhs)
        {
            dc.IsDeleted = true;
            dc.DeletedAt = now;
            dc.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho License / Bản quyền
        var licenses = await DbContext.Licenses
            .Where(l => idList.Contains(l.DuAnId) || (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value)))
            .ToListAsync();
        foreach (var l in licenses)
        {
            l.IsDeleted = true;
            l.DeletedAt = now;
            l.DeletedByUserId = userId;
        }

        // Cascade Soft Delete cho Hàng hóa dịch vụ thuộc Hợp đồng
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

        // Hủy trạng thái đã triển khai dự án nguồn nếu có
        foreach (var entity in entities)
        {
            if (entity.LoaiDuAn == 2 && !string.IsNullOrWhiteSpace(entity.NguonDuAnIds))
            {
                var sourceIds = entity.NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                                    .Where(g => g != Guid.Empty)
                                                    .ToList();
                if (sourceIds.Any())
                {
                    var sourceProjects = await DbSet.Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                    foreach (var sp in sourceProjects)
                    {
                        sp.DaTrienKhai = false;
                    }
                }
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

        // Khôi phục tất cả gói thầu liên quan bị xóa mềm
        var goiThaus = await DbContext.GoiThaus.IgnoreQueryFilters()
            .Where(gt => gt.DuAnId.HasValue && idList.Contains(gt.DuAnId.Value) && gt.IsDeleted)
            .ToListAsync();
        var goiThauIds = goiThaus.Select(gt => gt.Id).ToList();
        foreach (var gt in goiThaus)
        {
            gt.IsDeleted = false;
            gt.DeletedAt = null;
            gt.DeletedByUserId = null;
        }

        // Khôi phục tất cả hợp đồng liên quan bị xóa mềm
        var hopDongs = await DbContext.HopDongs.IgnoreQueryFilters()
            .Where(hd => ((hd.DuAnId.HasValue && idList.Contains(hd.DuAnId.Value)) || (hd.GoiThauId.HasValue && goiThauIds.Contains(hd.GoiThauId.Value))) && hd.IsDeleted)
            .ToListAsync();
        var hopDongIds = hopDongs.Select(hd => hd.Id).ToList();
        foreach (var hd in hopDongs)
        {
            hd.IsDeleted = false;
            hd.DeletedAt = null;
            hd.DeletedByUserId = null;
        }

        // Khôi phục tất cả công việc liên quan bị xóa mềm
        var congViecs = await DbContext.CongViecGoiThaus.IgnoreQueryFilters()
            .Where(cv => cv.GoiThau != null && cv.GoiThau.DuAnId.HasValue && idList.Contains(cv.GoiThau.DuAnId.Value) && cv.IsDeleted)
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

        var dieuChinhs = await DbContext.DieuChinhDuAns.IgnoreQueryFilters()
            .Where(dc => idList.Contains(dc.DuAnId) && dc.IsDeleted)
            .ToListAsync();
        foreach (var dc in dieuChinhs)
        {
            dc.IsDeleted = false;
            dc.DeletedAt = null;
            dc.DeletedByUserId = null;
        }

        var licenses = await DbContext.Licenses.IgnoreQueryFilters()
            .Where(l => (idList.Contains(l.DuAnId) || (l.HopDongId.HasValue && hopDongIds.Contains(l.HopDongId.Value))) && l.IsDeleted)
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

        // Đánh dấu lại trạng thái đã triển khai dự án nguồn nếu cần
        foreach (var entity in entities)
        {
            if (entity.LoaiDuAn == 2 && !string.IsNullOrWhiteSpace(entity.NguonDuAnIds))
            {
                var sourceIds = entity.NguonDuAnIds.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                                                    .Where(g => g != Guid.Empty)
                                                    .ToList();
                if (sourceIds.Any())
                {
                    var sourceProjects = await DbSet.IgnoreQueryFilters().Where(da => sourceIds.Contains(da.Id)).ToListAsync();
                    foreach (var sp in sourceProjects)
                    {
                        sp.DaTrienKhai = true;
                    }
                }
            }
        }

        await DbContext.SaveChangesAsync();
        return true;
    }
}

