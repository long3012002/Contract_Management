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
                    .ThenInclude(da => da.DanhSachNguonVon)
                        .ThenInclude(nv => nv.NguonVon)
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

        var dtos = items.Select(x => MapToDto(x, filter.DonViTinh)).ToList();

        return new PagedResult<KeHoachVonDto>
        {
            Items = dtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<KeHoachVonDto?> GetByIdAsync(Guid id, string? donViTinh = null)
    {
        var entity = await _context.KeHoachVons
            .Include(k => k.CreatedByUser)
            .Include(k => k.KeHoachVonDuAns)
                .ThenInclude(kd => kd.DuAn)
                    .ThenInclude(da => da.DanhSachNguonVon)
                        .ThenInclude(nv => nv.NguonVon)
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);

        return entity == null ? null : MapToDto(entity, donViTinh);
    }

    public async Task<KeHoachVonDto> CreateAsync(CreateKeHoachVonDto dto, Guid? currentUserId)
    {
        Guid? validUserId = null;
        if (currentUserId.HasValue && currentUserId.Value != Guid.Empty)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == currentUserId.Value);
            if (userExists)
            {
                validUserId = currentUserId.Value;
            }
        }

        var entity = new KeHoachVon
        {
            NamKeHoach = dto.NamKeHoach,
            LoaiKeHoach = dto.LoaiKeHoach,
            DotBoSung = dto.LoaiKeHoach == 3 ? (dto.DotBoSung ?? 1) : null,
            TrangThai = 3, // Mặc định Đã duyệt (vì thông tin nhập vào là dữ liệu đã được phê duyệt)
            SoQuyetDinh = dto.SoQuyetDinh,
            GhiChu = dto.GhiChu,
            CreatedByUserId = validUserId,
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

    public async Task<List<KeHoachVonDuAnItemDto>> GetLichSuKeHoachVonByDuAnIdAsync(Guid duAnId, string? donViTinh = null)
    {
        var factor = GetUnitFactor(donViTinh);
        var items = await _context.KeHoachVonDuAns
            .Include(kd => kd.KeHoachVon)
            .Include(kd => kd.DuAn)
                .ThenInclude(da => da!.DanhSachNguonVon)
                    .ThenInclude(nv => nv.NguonVon)
            .AsNoTracking()
            .Where(kd => kd.DuAnId == duAnId)
            .OrderByDescending(kd => kd.KeHoachVon.NamKeHoach)
            .ToListAsync();

        return items.Select(x =>
        {
            var namKhv = x.KeHoachVon?.NamKeHoach ?? 0;
            var allNv = x.DuAn?.DanhSachNguonVon ?? new List<DuAnNguonVon>();
            var hasNamMatches = allNv.Any(nv => nv.Nam == namKhv);
            var matchingNv = hasNamMatches
                ? allNv.Where(nv => nv.Nam == namKhv).ToList()
                : allNv.Where(nv => !nv.Nam.HasValue).ToList();

            var nguonVonChiTiet = matchingNv.Select(nv => new NguonVonChiTietItemDto
            {
                NguonVonId = nv.NguonVonId,
                MaNguonVon = nv.NguonVon?.Code ?? string.Empty,
                TenNguonVon = nv.NguonVon?.Name ?? string.Empty,
                SoTien = nv.SoTien / factor
            }).ToList();

            return new KeHoachVonDuAnItemDto
            {
                DuAnId = x.DuAnId,
                MaDuAn = x.DuAn?.Code ?? "",
                TenDuAn = x.DuAn?.Name ?? "",
                SoTienDeNghi = x.SoTienDeNghi / factor,
                SoTienDuocDuyet = x.SoTienDuocDuyet / factor,
                VonDieuLe = x.VonDieuLe.HasValue ? x.VonDieuLe.Value / factor : null,
                QuyDauTuPhatTrien = x.QuyDauTuPhatTrien.HasValue ? x.QuyDauTuPhatTrien.Value / factor : null,
                GhiChu = x.GhiChu ?? x.KeHoachVon?.SoQuyetDinh,
                NguonVonChiTiet = nguonVonChiTiet
            };
        }).ToList();
    }

    private static decimal GetUnitFactor(string? donViTinh)
    {
        if (string.IsNullOrWhiteSpace(donViTinh)) return 1m;
        var s = donViTinh.Trim().ToLowerInvariant();
        if (s == "4" || s.Contains("tỷ") || s.Contains("ty")) return 1_000_000_000m;
        if (s == "3" || s.Contains("triệu") || s.Contains("trieu")) return 1_000_000m;
        if (s == "2" || s.Contains("nghìn") || s.Contains("ngan") || s == "k") return 1_000m;
        return 1m;
    }

    private static KeHoachVonDto MapToDto(KeHoachVon k, string? donViTinh = null)
    {
        var factor = GetUnitFactor(donViTinh);

        var danhSachDuAn = k.KeHoachVonDuAns.Select(kd =>
        {
            var allNv = kd.DuAn?.DanhSachNguonVon ?? new List<DuAnNguonVon>();
            var hasNamMatches = allNv.Any(nv => nv.Nam == k.NamKeHoach);
            var matchingNv = hasNamMatches
                ? allNv.Where(nv => nv.Nam == k.NamKeHoach).ToList()
                : allNv.Where(nv => !nv.Nam.HasValue).ToList();

            var nguonVonChiTiet = matchingNv.Select(nv => new NguonVonChiTietItemDto
            {
                NguonVonId = nv.NguonVonId,
                MaNguonVon = nv.NguonVon?.Code ?? string.Empty,
                TenNguonVon = nv.NguonVon?.Name ?? string.Empty,
                SoTien = nv.SoTien / factor
            }).ToList();

            // Nếu dự án có VonDieuLe / QuyDauTuPhatTrien trực tiếp trên KeHoachVonDuAn
            if (kd.VonDieuLe.HasValue && kd.VonDieuLe.Value > 0 && !nguonVonChiTiet.Any(x => x.MaNguonVon == "VON_DIEU_LE"))
            {
                nguonVonChiTiet.Add(new NguonVonChiTietItemDto
                {
                    NguonVonId = Guid.Empty,
                    MaNguonVon = "VON_DIEU_LE",
                    TenNguonVon = "Vốn điều lệ & Quỹ dự trữ bổ sung vốn điều lệ",
                    SoTien = kd.VonDieuLe.Value / factor
                });
            }

            if (kd.QuyDauTuPhatTrien.HasValue && kd.QuyDauTuPhatTrien.Value > 0 && !nguonVonChiTiet.Any(x => x.MaNguonVon == "QUY_DTPT"))
            {
                nguonVonChiTiet.Add(new NguonVonChiTietItemDto
                {
                    NguonVonId = Guid.Empty,
                    MaNguonVon = "QUY_DTPT",
                    TenNguonVon = "Quỹ đầu tư phát triển",
                    SoTien = kd.QuyDauTuPhatTrien.Value / factor
                });
            }

            return new KeHoachVonDuAnItemDto
            {
                DuAnId = kd.DuAnId,
                MaDuAn = kd.DuAn?.Code ?? "",
                TenDuAn = kd.DuAn?.Name ?? "",
                SoTienDeNghi = kd.SoTienDeNghi / factor,
                SoTienDuocDuyet = kd.SoTienDuocDuyet / factor,
                VonDieuLe = kd.VonDieuLe.HasValue ? kd.VonDieuLe.Value / factor : null,
                QuyDauTuPhatTrien = kd.QuyDauTuPhatTrien.HasValue ? kd.QuyDauTuPhatTrien.Value / factor : null,
                GhiChu = kd.GhiChu,
                NguonVonChiTiet = nguonVonChiTiet
            };
        }).ToList();

        // Tính tổng theo nguồn vốn từ nguonVonChiTiet (nguồn vốn của dự án theo năm)
        var tongTheoNguonVon = danhSachDuAn
            .SelectMany(da => da.NguonVonChiTiet)
            .GroupBy(nv => new { nv.NguonVonId, nv.MaNguonVon, nv.TenNguonVon })
            .Select(g => new TongNguonVonItemDto
            {
                NguonVonId = g.Key.NguonVonId,
                MaNguonVon = g.Key.MaNguonVon,
                TenNguonVon = g.Key.TenNguonVon,
                TongSoTien = g.Sum(x => x.SoTien)
            }).ToList();

        // Tính tổng trực tiếp từ danh sách dự án, không đọc field cached trên entity
        var tongDeNghi = danhSachDuAn.Sum(da => da.SoTienDeNghi);
        var tongDuocDuyet = danhSachDuAn.Sum(da => da.SoTienDuocDuyet);

        return new KeHoachVonDto
        {
            Id = k.Id,
            NamKeHoach = k.NamKeHoach,
            LoaiKeHoach = k.LoaiKeHoach,
            DotBoSung = k.DotBoSung,
            TrangThai = k.TrangThai,
            SoQuyetDinh = k.SoQuyetDinh,
            NgayPheDuyet = k.NgayPheDuyet,
            // Ưu tiên giá trị tính thực tế, fallback về field cached nếu chưa có dự án nào
            TongMucDeNghi = tongDeNghi > 0 ? tongDeNghi : k.TongMucDeNghi / factor,
            TongMucDuocDuyet = tongDuocDuyet > 0 ? tongDuocDuyet : k.TongMucDuocDuyet / factor,
            GhiChu = k.GhiChu,
            CreatedByUserId = k.CreatedByUserId,
            CreatedByUserName = k.CreatedByUser?.FullName,
            CreatedAt = k.CreatedAt,
            DanhSachDuAn = danhSachDuAn,
            TongTheoNguonVon = tongTheoNguonVon
        };
    }
}
