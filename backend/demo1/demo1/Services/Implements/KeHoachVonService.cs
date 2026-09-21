using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class KeHoachVonService : IKeHoachVonService
{
    private readonly AppDbContext _context;

    public KeHoachVonService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<KeHoachVonDto>> GetAllAsync(KeHoachVonFilterDto filter)
    {
        var query = _context.KeHoachVons
            .Include(k => k.CreatedByUser)
            .Include(k => k.KeHoachVonDuAns)
                .ThenInclude(kd => kd.DuAn)
            .AsNoTracking()
            .AsQueryable();

        if (filter.Nam.HasValue)
        {
            query = query.Where(k => k.NamKeHoach == filter.Nam.Value);
        }

        if (filter.LoaiKeHoach.HasValue)
        {
            query = query.Where(k => k.LoaiKeHoach == filter.LoaiKeHoach.Value);
        }

        if (filter.TrangThai.HasValue)
        {
            query = query.Where(k => k.TrangThai == filter.TrangThai.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(k => (k.SoQuyetDinh != null && k.SoQuyetDinh.ToLower().Contains(search)) ||
                                     (k.GhiChu != null && k.GhiChu.ToLower().Contains(search)));
        }

        var totalItems = await query.CountAsync();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query
            .OrderByDescending(k => k.NamKeHoach)
            .ThenByDescending(k => k.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<KeHoachVonDto>
        {
            Items = dtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<KeHoachVonDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.KeHoachVons
            .Include(k => k.CreatedByUser)
            .Include(k => k.KeHoachVonDuAns)
                .ThenInclude(kd => kd.DuAn)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<KeHoachVonDto> CreateAsync(CreateKeHoachVonDto dto, Guid currentUserId)
    {
        var entity = new KeHoachVon
        {
            NamKeHoach = dto.NamKeHoach,
            LoaiKeHoach = dto.LoaiKeHoach,
            DotBoSung = dto.LoaiKeHoach == 3 ? (dto.DotBoSung ?? 1) : null,
            TrangThai = 3, // Mặc định Đã duyệt (vì thông tin nhập vào là dữ liệu đã được phê duyệt)
            SoQuyetDinh = dto.SoQuyetDinh,
            GhiChu = dto.GhiChu,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.DanhSachDuAn != null && dto.DanhSachDuAn.Any())
        {
            foreach (var da in dto.DanhSachDuAn)
            {
                var approvedAmount = (da.SoTienDuocDuyet.HasValue && da.SoTienDuocDuyet.Value > 0) ? da.SoTienDuocDuyet.Value : da.SoTienDeNghi;
                entity.KeHoachVonDuAns.Add(new KeHoachVonDuAn
                {
                    DuAnId = da.DuAnId,
                    SoTienDeNghi = da.SoTienDeNghi,
                    SoTienDuocDuyet = approvedAmount,
                    VonDieuLe = da.VonDieuLe,
                    QuyDauTuPhatTrien = da.QuyDauTuPhatTrien,
                    GhiChu = da.GhiChu,
                    CreatedAt = DateTime.UtcNow
                });
            }

            entity.TongMucDeNghi = entity.KeHoachVonDuAns.Sum(x => x.SoTienDeNghi);
            entity.TongMucDuocDuyet = entity.KeHoachVonDuAns.Sum(x => x.SoTienDuocDuyet);
        }

        _context.KeHoachVons.Add(entity);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateKeHoachVonDto dto)
    {
        var entity = await _context.KeHoachVons.FirstOrDefaultAsync(k => k.Id == id);
        if (entity == null) return false;

        entity.NamKeHoach = dto.NamKeHoach;
        entity.LoaiKeHoach = dto.LoaiKeHoach;
        entity.DotBoSung = dto.LoaiKeHoach == 3 ? (dto.DotBoSung ?? 1) : null;
        entity.SoQuyetDinh = dto.SoQuyetDinh;
        entity.GhiChu = dto.GhiChu;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.KeHoachVons.FirstOrDefaultAsync(k => k.Id == id);
        if (entity == null) return false;

        _context.KeHoachVons.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<KeHoachVonDto> SubmitAsync(Guid id)
    {
        var entity = await _context.KeHoachVons.FirstOrDefaultAsync(k => k.Id == id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Kế hoạch vốn với ID [{id}].");
        }

        if (entity.TrangThai != 1)
        {
            throw new InvalidOperationException("Kế hoạch vốn phải ở trạng thái Bản nháp mới có thể Trình duyệt.");
        }

        entity.TrangThai = 2; // Submitted
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<KeHoachVonDto> ApproveAsync(Guid id, ApproveKeHoachVonDto dto)
    {
        var entity = await _context.KeHoachVons
            .Include(k => k.KeHoachVonDuAns)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Kế hoạch vốn với ID [{id}].");
        }

        if (entity.TrangThai != 2) // Submitted
        {
            throw new InvalidOperationException("Kế hoạch vốn phải ở trạng thái Đã trình duyệt mới có thể Phê duyệt.");
        }

        if (string.IsNullOrWhiteSpace(dto.SoQuyetDinh))
        {
            throw new ArgumentException("Vui lòng nhập Số quyết định phê duyệt Kế hoạch vốn.");
        }

        entity.SoQuyetDinh = dto.SoQuyetDinh.Trim();
        entity.NgayPheDuyet = dto.NgayPheDuyet;
        entity.TrangThai = 3; // Approved
        entity.UpdatedAt = DateTime.UtcNow;

        if (dto.DanhSachDuAnDuyet != null && dto.DanhSachDuAnDuyet.Any())
        {
            foreach (var item in dto.DanhSachDuAnDuyet)
            {
                var target = entity.KeHoachVonDuAns.FirstOrDefault(x => x.DuAnId == item.DuAnId);
                if (target != null)
                {
                    target.SoTienDuocDuyet = item.SoTienDuocDuyet;
                    if (item.VonDieuLe.HasValue) target.VonDieuLe = item.VonDieuLe.Value;
                    if (item.QuyDauTuPhatTrien.HasValue) target.QuyDauTuPhatTrien = item.QuyDauTuPhatTrien.Value;
                    if (!string.IsNullOrWhiteSpace(item.GhiChu)) target.GhiChu = item.GhiChu;
                }
            }
        }

        entity.TongMucDuocDuyet = entity.KeHoachVonDuAns.Sum(x => x.SoTienDuocDuyet);

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<KeHoachVonDto> RejectAsync(Guid id, string? ghiChu)
    {
        var entity = await _context.KeHoachVons.FirstOrDefaultAsync(k => k.Id == id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Kế hoạch vốn với ID [{id}].");
        }

        if (entity.TrangThai != 2)
        {
            throw new InvalidOperationException("Chỉ có thể Trả về Kế hoạch vốn khi đang ở trạng thái Đã trình.");
        }

        entity.TrangThai = 4; // Rejected
        if (!string.IsNullOrWhiteSpace(ghiChu))
        {
            entity.GhiChu = (string.IsNullOrWhiteSpace(entity.GhiChu) ? "" : entity.GhiChu + "\n") + $"[Lý do trả về]: {ghiChu}";
        }
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<KeHoachVonDto> AddOrUpdateDuAnAsync(Guid id, AddDuAnToKHVDto dto)
    {
        var entity = await _context.KeHoachVons
            .Include(k => k.KeHoachVonDuAns)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Kế hoạch vốn với ID [{id}].");
        }

        var existing = entity.KeHoachVonDuAns.FirstOrDefault(x => x.DuAnId == dto.DuAnId);
        var approvedAmount = dto.SoTienDeNghi;
        if (existing != null)
        {
            existing.SoTienDeNghi = dto.SoTienDeNghi;
            existing.SoTienDuocDuyet = approvedAmount;
            existing.VonDieuLe = dto.VonDieuLe;
            existing.QuyDauTuPhatTrien = dto.QuyDauTuPhatTrien;
            existing.GhiChu = dto.GhiChu;
        }
        else
        {
            entity.KeHoachVonDuAns.Add(new KeHoachVonDuAn
            {
                KeHoachVonId = id,
                DuAnId = dto.DuAnId,
                SoTienDeNghi = dto.SoTienDeNghi,
                SoTienDuocDuyet = approvedAmount,
                VonDieuLe = dto.VonDieuLe,
                QuyDauTuPhatTrien = dto.QuyDauTuPhatTrien,
                GhiChu = dto.GhiChu,
                CreatedAt = DateTime.UtcNow
            });
        }

        entity.TongMucDeNghi = entity.KeHoachVonDuAns.Sum(x => x.SoTienDeNghi);
        entity.TongMucDuocDuyet = entity.KeHoachVonDuAns.Sum(x => x.SoTienDuocDuyet);
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<KeHoachVonDto> RemoveDuAnAsync(Guid id, Guid duAnId)
    {
        var entity = await _context.KeHoachVons
            .Include(k => k.KeHoachVonDuAns)
            .FirstOrDefaultAsync(k => k.Id == id);

        if (entity == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy Kế hoạch vốn với ID [{id}].");
        }

        var existing = entity.KeHoachVonDuAns.FirstOrDefault(x => x.DuAnId == duAnId);
        if (existing != null)
        {
            entity.KeHoachVonDuAns.Remove(existing);
            entity.TongMucDeNghi = entity.KeHoachVonDuAns.Sum(x => x.SoTienDeNghi);
            entity.TongMucDuocDuyet = entity.KeHoachVonDuAns.Sum(x => x.SoTienDuocDuyet);
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return (await GetByIdAsync(id))!;
    }

    public async Task<List<KeHoachVonDuAnItemDto>> GetLichSuKeHoachVonByDuAnIdAsync(Guid duAnId)
    {
        var items = await _context.KeHoachVonDuAns
            .Include(kd => kd.KeHoachVon)
            .Include(kd => kd.DuAn)
            .AsNoTracking()
            .Where(kd => kd.DuAnId == duAnId)
            .OrderByDescending(kd => kd.KeHoachVon.NamKeHoach)
            .ToListAsync();

        return items.Select(x => new KeHoachVonDuAnItemDto
        {
            DuAnId = x.DuAnId,
            MaDuAn = x.DuAn?.Code ?? "",
            TenDuAn = x.DuAn?.Name ?? "",
            SoTienDeNghi = x.SoTienDeNghi,
            SoTienDuocDuyet = x.SoTienDuocDuyet,
            VonDieuLe = x.VonDieuLe,
            QuyDauTuPhatTrien = x.QuyDauTuPhatTrien,
            GhiChu = x.GhiChu ?? x.KeHoachVon?.SoQuyetDinh
        }).ToList();
    }

    private static KeHoachVonDto MapToDto(KeHoachVon k)
    {
        return new KeHoachVonDto
        {
            Id = k.Id,
            NamKeHoach = k.NamKeHoach,
            LoaiKeHoach = k.LoaiKeHoach,
            DotBoSung = k.DotBoSung,
            TrangThai = k.TrangThai,
            SoQuyetDinh = k.SoQuyetDinh,
            NgayPheDuyet = k.NgayPheDuyet,
            TongMucDeNghi = k.TongMucDeNghi,
            TongMucDuocDuyet = k.TongMucDuocDuyet,
            GhiChu = k.GhiChu,
            CreatedByUserId = k.CreatedByUserId,
            CreatedByUserName = k.CreatedByUser?.FullName,
            CreatedAt = k.CreatedAt,
            DanhSachDuAn = k.KeHoachVonDuAns.Select(kd => new KeHoachVonDuAnItemDto
            {
                DuAnId = kd.DuAnId,
                MaDuAn = kd.DuAn?.Code ?? "",
                TenDuAn = kd.DuAn?.Name ?? "",
                SoTienDeNghi = kd.SoTienDeNghi,
                SoTienDuocDuyet = kd.SoTienDuocDuyet,
                VonDieuLe = kd.VonDieuLe,
                QuyDauTuPhatTrien = kd.QuyDauTuPhatTrien,
                GhiChu = kd.GhiChu
            }).ToList()
        };
    }
}
