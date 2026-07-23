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

public class HopDongService : DbCrudService<HopDong, HopDongDto, CreateHopDongDto, UpdateHopDongDto>, IHopDongService
{
    public HopDongService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public override async Task<PagedResult<HopDongDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<HopDong> query = DbSet.AsNoTracking()
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .Include(h => h.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(item => 
                EF.Functions.Like(item.Code, $"%{keyword}%") || 
                EF.Functions.Like(item.Name, $"%{keyword}%") ||
                (item.Description != null && EF.Functions.Like(item.Description, $"%{keyword}%")));
        }

        var totalItems = await query.CountAsync();

        List<HopDong> items;
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

        var dtos = Mapper.Map<List<HopDongDto>>(items);

        return new PagedResult<HopDongDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    public override async Task<IReadOnlyList<HopDongDto>> GetAllItemsAsync()
    {
        var items = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .Include(h => h.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .ToListAsync();
        return Mapper.Map<List<HopDongDto>>(items);
    }

    public override async Task<HopDongDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .Include(h => h.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .FirstOrDefaultAsync(h => h.Id == id);
        return entity is null ? null : Mapper.Map<HopDongDto>(entity);
    }

    public override async Task<HopDongDto> CreateAsync(CreateHopDongDto dto)
    {
        HopDongValidator.EnsureValid(dto.GiaTriHopDong, dto.DotThanhToans);

        // Check NhaThauGoiThaus existence and validate
        if (dto.NhaThauGoiThaus != null && dto.NhaThauGoiThaus.Any())
        {
            var bidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).Distinct().ToList();
            var existingCount = await DbContext.DoiTacs.CountAsync(dt => bidderIds.Contains(dt.Id));
            if (existingCount != bidderIds.Count)
            {
                throw new KeyNotFoundException("Một hoặc nhiều nhà thầu được chọn không tồn tại.");
            }

            // Normalize for single bidder if needed
            if (dto.NhaThauGoiThaus.Count == 1)
            {
                var single = dto.NhaThauGoiThaus.First();
                single.IsLienDanh = false;
                single.TenLienDanh = null;
                single.IsDaiDienLienDanh = false;
                single.TyLeLienDanh = 100;
                single.GiaTriDamNhan = dto.GiaTriHopDong;
            }

            // Validate
            HopDongValidator.ValidateBidders(dto.GiaTriHopDong, dto.NhaThauGoiThaus);
        }

        // Check DuAn existence
        if (dto.DuAnId.HasValue)
        {
            var duAnExists = await DbContext.DuAns.AnyAsync(da => da.Id == dto.DuAnId.Value);
            if (!duAnExists)
            {
                throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
            }
        }

        // Check GoiThau uniqueness for contracts
        if (dto.GoiThauId.HasValue)
        {
            var goiThau = await DbContext.GoiThaus.FirstOrDefaultAsync(gt => gt.Id == dto.GoiThauId.Value);
            if (goiThau == null)
            {
                throw new KeyNotFoundException("Không tìm thấy gói thầu được liên kết.");
            }

            var alreadyLinked = await DbSet.AnyAsync(h => h.GoiThauId == dto.GoiThauId.Value);
            if (alreadyLinked)
            {
                throw new InvalidOperationException("Gói thầu này đã được liên kết với một hợp đồng khác.");
            }

            if (dto.GiaTriHopDong > goiThau.GiaTriGoiThau)
            {
                throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được lớn hơn giá trị dự toán của gói thầu ({goiThau.GiaTriGoiThau:N0} VNĐ).");
            }
        }

        // Check ChuDauTu and NhaThau existence
        if (dto.ChuDauTuId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.ChuDauTuId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin chủ đầu tư.");
        }
        if (dto.NhaThauId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaThauId.Value))
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin nhà thầu.");
        }

        var entity = Mapper.Map<HopDong>(dto);
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        if (dto.NhaThauGoiThaus != null)
        {
            foreach (var inputDto in dto.NhaThauGoiThaus)
            {
                var nt = Mapper.Map<NhaThauGoiThau>(inputDto);
                nt.HopDongId = entity.Id;
                entity.NhaThauGoiThaus.Add(nt);
            }
        }

        // Ensure unique code
        var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == entity.Code.ToLower());
        if (exists)
        {
            throw new InvalidOperationException($"Số ký hiệu hợp đồng '{entity.Code}' đã tồn tại.");
        }

        // Add payment installments
        if (dto.DotThanhToans != null)
        {
            var now = DateTime.UtcNow;
            int index = 0;
            foreach (var dotDto in dto.DotThanhToans)
            {
                var dot = Mapper.Map<DotThanhToan>(dotDto);
                dot.Id = Guid.NewGuid();
                dot.HopDongId = entity.Id;
                // Use user-provided payment value if set, otherwise calculate based on percentage
                dot.GiaTriThanhToan = dotDto.GiaTriThanhToan > 0 ? dotDto.GiaTriThanhToan : (dot.TyLeThanhToan * entity.GiaTriHopDong / 100);
                dot.NgayThanhToan = dotDto.NgayThanhToan;
                dot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                dot.CreatedAt = now.AddMilliseconds(index++);
                entity.DotThanhToans.Add(dot);
            }
        }

        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        var reloaded = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .Include(h => h.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .FirstOrDefaultAsync(h => h.Id == entity.Id);

        return Mapper.Map<HopDongDto>(reloaded);
    }

    public override async Task<IEnumerable<HopDongDto>> CreateRangeAsync(IEnumerable<CreateHopDongDto> dtos)
    {
        var dtoList = dtos.ToList();
        if (!dtoList.Any()) return Enumerable.Empty<HopDongDto>();

        // 1. Xác thực các hợp đồng locally
        foreach (var dto in dtoList)
        {
            HopDongValidator.EnsureValid(dto.GiaTriHopDong, dto.DotThanhToans);
        }

        // 2. Kiểm tra tính duy nhất của mã hợp đồng theo lô
        var incomingCodes = dtoList.Select(d => d.Code.Trim().ToLower()).Distinct().ToList();
        var existingCodes = await DbSet
            .Where(item => incomingCodes.Contains(item.Code.ToLower()))
            .Select(item => item.Code.ToLower())
            .ToListAsync();

        if (existingCodes.Any())
        {
            throw new InvalidOperationException($"Các số ký hiệu hợp đồng sau đã tồn tại: {string.Join(", ", existingCodes)}");
        }

        // 3. Kiểm tra tính tồn tại của Dự án liên kết
        var duAnIds = dtoList.Where(d => d.DuAnId.HasValue).Select(d => d.DuAnId!.Value).Distinct().ToList();
        if (duAnIds.Any())
        {
            var existingDuAnCount = await DbContext.DuAns.CountAsync(da => duAnIds.Contains(da.Id));
            if (existingDuAnCount != duAnIds.Count)
            {
                throw new KeyNotFoundException("Một số dự án được liên kết không tồn tại.");
            }
        }

        // 4. Kiểm tra tính tồn tại của Đối tác (Chủ đầu tư, Nhà thầu & các Nhà thầu trong NhaThauGoiThaus)
        var chuDauTuIds = dtoList.Where(d => d.ChuDauTuId.HasValue).Select(d => d.ChuDauTuId!.Value).Distinct().ToList();
        var nhaThauIds = dtoList.Where(d => d.NhaThauId.HasValue).Select(d => d.NhaThauId!.Value).Distinct().ToList();
        var bidderIds = dtoList.Where(d => d.NhaThauGoiThaus != null).SelectMany(d => d.NhaThauGoiThaus!).Select(b => b.NhaThauId).Distinct().ToList();
        var allDoiTacIds = chuDauTuIds.Concat(nhaThauIds).Concat(bidderIds).Distinct().ToList();
        if (allDoiTacIds.Any())
        {
            var existingDoiTacCount = await DbContext.DoiTacs.CountAsync(dt => allDoiTacIds.Contains(dt.Id));
            if (existingDoiTacCount != allDoiTacIds.Count)
            {
                throw new KeyNotFoundException("Một số đối tác (chủ đầu tư, nhà thầu hoặc thành viên liên danh) được liên kết không tồn tại.");
            }
        }

        // 5. Kiểm tra ràng buộc duy nhất và giới hạn giá trị của Gói thầu liên kết
        var goiThauIds = dtoList.Where(d => d.GoiThauId.HasValue).Select(d => d.GoiThauId!.Value).Distinct().ToList();
        List<GoiThau> goiThaus = new List<GoiThau>();
        if (goiThauIds.Any())
        {
            goiThaus = await DbContext.GoiThaus.Where(gt => goiThauIds.Contains(gt.Id)).ToListAsync();
            if (goiThaus.Count != goiThauIds.Count)
            {
                throw new KeyNotFoundException("Một số gói thầu được liên kết không tồn tại.");
            }

            // Kiểm tra xem các gói thầu này đã được liên kết với hợp đồng khác trong DB chưa
            var linkedGoiThauIds = await DbSet
                .Where(h => h.GoiThauId.HasValue && goiThauIds.Contains(h.GoiThauId.Value))
                .Select(h => h.GoiThauId!.Value)
                .ToListAsync();

            if (linkedGoiThauIds.Any())
            {
                throw new InvalidOperationException("Một số gói thầu đã được liên kết với hợp đồng khác.");
            }

            // Kiểm tra trùng lặp gói thầu trong lô gửi lên
            if (goiThauIds.Count < dtoList.Count(d => d.GoiThauId.HasValue))
            {
                throw new InvalidOperationException("Không thể liên kết nhiều hợp đồng với cùng một gói thầu trong cùng một lượt tạo.");
            }
        }

        var entities = new List<HopDong>();
        var now = DateTime.UtcNow;
        int dotIndex = 0;

        foreach (var dto in dtoList)
        {
            if (dto.GoiThauId.HasValue)
            {
                var goiThau = goiThaus.First(gt => gt.Id == dto.GoiThauId.Value);
                if (dto.GiaTriHopDong > goiThau.GiaTriGoiThau)
                {
                    throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được lớn hơn giá trị dự toán của gói thầu '{goiThau.Name}' ({goiThau.GiaTriGoiThau:N0} VNĐ).");
                }
            }

            var entity = Mapper.Map<HopDong>(dto);
            entity.Id = Guid.NewGuid();
            entity.CreatedAt = now;

            // Ánh xạ các đợt thanh toán (DotThanhToan)
            if (dto.DotThanhToans != null)
            {
                foreach (var dotDto in dto.DotThanhToans)
                {
                    var dot = Mapper.Map<DotThanhToan>(dotDto);
                    dot.Id = Guid.NewGuid();
                    dot.HopDongId = entity.Id;
                    dot.GiaTriThanhToan = dotDto.GiaTriThanhToan > 0 ? dotDto.GiaTriThanhToan : (dot.TyLeThanhToan * entity.GiaTriHopDong / 100);
                    dot.NgayThanhToan = dotDto.NgayThanhToan;
                    dot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                    dot.CreatedAt = now.AddMilliseconds(dotIndex++);
                    entity.DotThanhToans.Add(dot);
                }
            }

            // Ánh xạ các nhà thầu liên kết (NhaThauGoiThau)
            if (dto.NhaThauGoiThaus != null && dto.NhaThauGoiThaus.Any())
            {
                // Normalize for single bidder if needed
                if (dto.NhaThauGoiThaus.Count == 1)
                {
                    var single = dto.NhaThauGoiThaus.First();
                    single.IsLienDanh = false;
                    single.TenLienDanh = null;
                    single.IsDaiDienLienDanh = false;
                    single.TyLeLienDanh = 100;
                    single.GiaTriDamNhan = dto.GiaTriHopDong;
                }

                HopDongValidator.ValidateBidders(dto.GiaTriHopDong, dto.NhaThauGoiThaus);

                foreach (var inputDto in dto.NhaThauGoiThaus)
                {
                    var nt = Mapper.Map<NhaThauGoiThau>(inputDto);
                    nt.HopDongId = entity.Id;
                    entity.NhaThauGoiThaus.Add(nt);
                }
            }

            entities.Add(entity);
        }

        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync(); // Chỉ gọi SaveChanges 1 lần duy nhất

        // Nạp lại dữ liệu đầy đủ kèm các Include để trả về DTO đồng bộ
        var reloadedIds = entities.Select(e => e.Id).ToList();
        var reloadedEntities = await DbSet
            .Include(h => h.GoiThau)
            .Include(h => h.DuAn)
            .Include(h => h.ChuDauTu)
            .Include(h => h.NhaThau)
            .Include(h => h.DotThanhToans)
            .Include(h => h.NhaThauGoiThaus)
                .ThenInclude(nt => nt.NhaThau)
            .Where(h => reloadedIds.Contains(h.Id))
            .ToListAsync();

        return Mapper.Map<List<HopDongDto>>(reloadedEntities);
    }

    public override async Task<bool> UpdateAsync(Guid id, UpdateHopDongDto dto)
    {
        using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            var entity = await DbSet.FirstOrDefaultAsync(h => h.Id == id);
            if (entity is null)
            {
                return false;
            }

            HopDongValidator.EnsureValid(dto.GiaTriHopDong, dto.DotThanhToans);

            if (dto.DotThanhToans != null && dto.DotThanhToans.Any())
            {
                var totalPaymentVal = dto.DotThanhToans.Sum(d => d.GiaTriThanhToan > 0 ? d.GiaTriThanhToan : (d.TyLeThanhToan * dto.GiaTriHopDong / 100));
                if (dto.GiaTriHopDong < totalPaymentVal)
                {
                    throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được nhỏ hơn tổng giá trị của các đợt thanh toán ({totalPaymentVal:N0} VNĐ).");
                }
            }

            // Ensure unique code
            var exists = await DbSet.AnyAsync(item => item.Code.ToLower() == dto.Code.ToLower() && item.Id != id);
            if (exists)
            {
                throw new InvalidOperationException($"Số ký hiệu hợp đồng '{dto.Code}' đã tồn tại.");
            }

            // Check DuAn existence
            if (dto.DuAnId.HasValue)
            {
                var duAnExists = await DbContext.DuAns.AnyAsync(da => da.Id == dto.DuAnId.Value);
                if (!duAnExists)
                {
                    throw new KeyNotFoundException("Không tìm thấy dự án được liên kết.");
                }
            }

            // Check GoiThau uniqueness for contracts
            if (dto.GoiThauId.HasValue)
            {
                var goiThau = await DbContext.GoiThaus.FirstOrDefaultAsync(gt => gt.Id == dto.GoiThauId.Value);
                if (goiThau == null)
                {
                    throw new KeyNotFoundException("Không tìm thấy gói thầu được liên kết.");
                }

                var alreadyLinked = await DbSet.AnyAsync(h => h.GoiThauId == dto.GoiThauId.Value && h.Id != id);
                if (alreadyLinked)
                {
                    throw new InvalidOperationException("Gói thầu này đã được liên kết với một hợp đồng khác.");
                }

                if (dto.GiaTriHopDong > goiThau.GiaTriGoiThau)
                {
                    throw new InvalidOperationException($"Giá trị hợp đồng ({dto.GiaTriHopDong:N0} VNĐ) không được lớn hơn giá trị dự toán của gói thầu ({goiThau.GiaTriGoiThau:N0} VNĐ).");
                }
            }

            // Check ChuDauTu and NhaThau existence
            if (dto.ChuDauTuId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.ChuDauTuId.Value))
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin chủ đầu tư.");
            }
            if (dto.NhaThauId.HasValue && !await DbContext.DoiTacs.AnyAsync(dt => dt.Id == dto.NhaThauId.Value))
            {
                throw new KeyNotFoundException("Không tìm thấy thông tin nhà thầu.");
            }

            // Check NhaThauGoiThaus existence and validate
            if (dto.NhaThauGoiThaus != null)
            {
                var bidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).Distinct().ToList();
                if (bidderIds.Any())
                {
                    var existingCount = await DbContext.DoiTacs.CountAsync(dt => bidderIds.Contains(dt.Id));
                    if (existingCount != bidderIds.Count)
                    {
                        throw new KeyNotFoundException("Một hoặc nhiều nhà thầu được chọn không tồn tại.");
                    }
                }

                // Normalize for single bidder if needed
                if (dto.NhaThauGoiThaus.Count == 1)
                {
                    var single = dto.NhaThauGoiThaus.First();
                    single.IsLienDanh = false;
                    single.TenLienDanh = null;
                    single.IsDaiDienLienDanh = false;
                    single.TyLeLienDanh = 100;
                    single.GiaTriDamNhan = dto.GiaTriHopDong;
                }

                // Validate
                HopDongValidator.ValidateBidders(dto.GiaTriHopDong, dto.NhaThauGoiThaus);
            }

            Mapper.Map(dto, entity);
            entity.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            // Load existing payment installments
            var existingDots = await DbContext.DotThanhToans.Where(d => d.HopDongId == id).ToListAsync();

            // Delete existing ones not present in DTO
            var incomingIds = dto.DotThanhToans?.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList() ?? new List<Guid>();
            var dotsToDelete = existingDots.Where(d => !incomingIds.Contains(d.Id)).ToList();
            if (dotsToDelete.Any())
            {
                DbContext.DotThanhToans.RemoveRange(dotsToDelete);
            }

            // Add or Update incoming installments
            if (dto.DotThanhToans != null)
            {
                var now = DateTime.UtcNow;
                int index = 0;
                foreach (var dotDto in dto.DotThanhToans)
                {
                    if (dotDto.Id.HasValue)
                    {
                        // Update existing
                        var existingDot = existingDots.FirstOrDefault(d => d.Id == dotDto.Id.Value);
                        if (existingDot != null)
                        {
                            Mapper.Map(dotDto, existingDot);
                            // Preserve user-provided value, calculate as fallback
                            existingDot.GiaTriThanhToan = dotDto.GiaTriThanhToan > 0 ? dotDto.GiaTriThanhToan : (existingDot.TyLeThanhToan * entity.GiaTriHopDong / 100);
                            existingDot.NgayThanhToan = dotDto.NgayThanhToan;
                            existingDot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                            existingDot.UpdatedAt = now;
                        }
                    }
                    else
                    {
                        // Add new
                        var dot = Mapper.Map<DotThanhToan>(dotDto);
                        dot.Id = Guid.NewGuid();
                        dot.HopDongId = id;
                        // Preserve user-provided value, calculate as fallback
                        dot.GiaTriThanhToan = dotDto.GiaTriThanhToan > 0 ? dotDto.GiaTriThanhToan : (dot.TyLeThanhToan * entity.GiaTriHopDong / 100);
                        dot.NgayThanhToan = dotDto.NgayThanhToan;
                        dot.DieuKienThanhToan = dotDto.DieuKienThanhToan;
                        dot.CreatedAt = now.AddMilliseconds(index++);
                        await DbContext.DotThanhToans.AddAsync(dot);
                    }
                }
            }

            // Process NhaThauGoiThaus updates
            if (dto.NhaThauGoiThaus != null)
            {
                // Fetch existing
                var existingBidders = await DbContext.NhaThauGoiThaus
                    .Where(nt => nt.HopDongId == id)
                    .ToListAsync();

                // 1. Remove deleted
                var incomingBidderIds = dto.NhaThauGoiThaus.Select(b => b.NhaThauId).ToHashSet();
                var toRemove = existingBidders.Where(eb => !incomingBidderIds.Contains(eb.NhaThauId)).ToList();
                DbContext.NhaThauGoiThaus.RemoveRange(toRemove);

                // 2. Add or Update
                foreach (var inputDto in dto.NhaThauGoiThaus)
                {
                    var existing = existingBidders.FirstOrDefault(eb => eb.NhaThauId == inputDto.NhaThauId);
                    if (existing == null)
                    {
                        var newNt = Mapper.Map<NhaThauGoiThau>(inputDto);
                        newNt.HopDongId = id;
                        await DbContext.NhaThauGoiThaus.AddAsync(newNt);
                    }
                    else
                    {
                        existing.IsLienDanh = inputDto.IsLienDanh;
                        existing.TenLienDanh = inputDto.TenLienDanh;
                        existing.IsDaiDienLienDanh = inputDto.IsDaiDienLienDanh ?? false;
                        existing.TyLeLienDanh = inputDto.TyLeLienDanh;
                        existing.GiaTriDamNhan = inputDto.GiaTriDamNhan;
                        existing.VaiTroTrongLienDanh = inputDto.VaiTroTrongLienDanh;
                        existing.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ConfirmPaymentAsync(Guid dotThanhToanId)
    {
        var dotThanhToan = await DbContext.DotThanhToans.FirstOrDefaultAsync(d => d.Id == dotThanhToanId);
        if (dotThanhToan == null)
        {
            return false;
        }

        dotThanhToan.IsPaid = true;
        dotThanhToan.UpdatedAt = DateTime.UtcNow;
        if (dotThanhToan.NgayThanhToan == null)
        {
            dotThanhToan.NgayThanhToan = DateTime.UtcNow;
        }

        await DbContext.SaveChangesAsync();
        return true;
    }
}
