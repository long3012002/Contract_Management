using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements;

public class PhuLucHopDongService : IPhuLucHopDongService
{
    private readonly AppDbContext _context;

    public PhuLucHopDongService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PhuLucHopDongDto>> GetByHopDongIdAsync(Guid hopDongId, TrangThaiPhuLuc? trangThai = null)
    {
        var query = _context.PhuLucHopDongs
            .AsNoTracking()
            .Include(p => p.DotThanhToans)
            .Include(p => p.HangHoaDichVus)
            .Where(p => p.HopDongId == hopDongId);

        if (trangThai.HasValue)
        {
            query = query.Where(p => p.TrangThai == trangThai.Value);
        }

        var list = await query.OrderByDescending(p => p.NgayKy).ThenByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<PhuLucHopDongDto?> GetByIdAsync(Guid id)
    {
        var item = await _context.PhuLucHopDongs
            .AsNoTracking()
            .Include(p => p.DotThanhToans)
            .Include(p => p.HangHoaDichVus)
            .FirstOrDefaultAsync(p => p.Id == id);

        return item == null ? null : MapToDto(item);
    }

    public async Task<PhuLucHopDongDto> CreateAsync(CreatePhuLucHopDongDto dto)
    {
        var hopDong = await _context.HopDongs.FindAsync(dto.HopDongId);
        if (hopDong == null)
        {
            throw new ArgumentException("Hợp đồng không tồn tại.");
        }

        var entity = new PhuLucHopDong
        {
            Id = Guid.NewGuid(),
            HopDongId = dto.HopDongId,
            Code = dto.SoPhuLuc,
            Name = dto.TenPhuLuc,
            SoPhuLuc = dto.SoPhuLuc,
            TenPhuLuc = dto.TenPhuLuc,
            LoaiPhuLuc = dto.LoaiPhuLuc,
            TrangThai = dto.TrangThai,
            NgayKy = dto.NgayKy,
            NgayHieuLuc = dto.NgayHieuLuc,
            GiaTriDieuChinh = dto.GiaTriDieuChinh,
            ExpiredDateMoi = dto.ExpiredDateMoi,
            ThoiHanThucHienMoi = dto.ThoiHanThucHienMoi,
            NoiDungDieuChinh = dto.NoiDungDieuChinh,
            GhiChu = dto.GhiChu,
            CreatedAt = DateTime.UtcNow
        };

        _context.PhuLucHopDongs.Add(entity);

        // Process DotThanhToans
        if (dto.DotThanhToans != null && dto.DotThanhToans.Any())
        {
            foreach (var dot in dto.DotThanhToans)
            {
                var dotEntity = new DotThanhToan
                {
                    Id = Guid.NewGuid(),
                    HopDongId = dto.HopDongId,
                    PhuLucHopDongId = entity.Id,
                    TenDot = dot.TenDot,
                    TyLeThanhToan = dot.TyLeThanhToan,
                    GiaTriThanhToan = dot.GiaTriThanhToan,
                    NgayThanhToan = dot.NgayThanhToan,
                    DieuKienThanhToan = dot.DieuKienThanhToan,
                    IsPaid = dot.IsPaid,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DotThanhToans.Add(dotEntity);
            }
        }

        // Process HangHoaDichVus
        if (dto.HangHoaDichVus != null && dto.HangHoaDichVus.Any())
        {
            foreach (var item in dto.HangHoaDichVus)
            {
                var hEntity = new HangHoaDichVu
                {
                    Id = Guid.NewGuid(),
                    Code = $"PLHH-{Guid.NewGuid().ToString()[..8]}",
                    Name = item.DanhMucHangHoa ?? item.TenDichVu ?? "Hàng hóa/Dịch vụ phụ lục",
                    IdParent = dto.HopDongId,
                    PhuLucHopDongId = entity.Id,
                    Loai = item.Loai,
                    KhoiLuong = item.KhoiLuong,
                    DonGia = item.DonGia,
                    ThanhTien = item.ThanhTien,
                    Stt = item.Stt,
                    DanhMucHangHoa = item.DanhMucHangHoa,
                    KyMaHieu = item.KyMaHieu,
                    NhanHieu = item.NhanHieu,
                    NamSanXuat = item.NamSanXuat,
                    IdXuatXu = item.IdXuatXu,
                    IdHangSanXuat = item.IdHangSanXuat,
                    IdLicense = item.IdLicense,
                    IdDonViTinh = item.IdDonViTinh,
                    TenDichVu = item.TenDichVu,
                    MoTaDichVu = item.MoTaDichVu,
                    CreatedAt = DateTime.UtcNow
                };
                _context.HangHoaDichVus.Add(hEntity);
            }
        }

        await _context.SaveChangesAsync();

        await RecalculateContractFromAddendumsAsync(dto.HopDongId);

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<PhuLucHopDongDto?> UpdateAsync(Guid id, UpdatePhuLucHopDongDto dto)
    {
        var entity = await _context.PhuLucHopDongs.FindAsync(id);
        if (entity == null) return null;

        entity.SoPhuLuc = dto.SoPhuLuc;
        entity.TenPhuLuc = dto.TenPhuLuc;
        entity.Code = dto.SoPhuLuc;
        entity.Name = dto.TenPhuLuc;
        entity.LoaiPhuLuc = dto.LoaiPhuLuc;
        entity.TrangThai = dto.TrangThai;
        entity.NgayKy = dto.NgayKy;
        entity.NgayHieuLuc = dto.NgayHieuLuc;
        entity.GiaTriDieuChinh = dto.GiaTriDieuChinh;
        entity.ExpiredDateMoi = dto.ExpiredDateMoi;
        entity.ThoiHanThucHienMoi = dto.ThoiHanThucHienMoi;
        entity.NoiDungDieuChinh = dto.NoiDungDieuChinh;
        entity.GhiChu = dto.GhiChu;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RecalculateContractFromAddendumsAsync(entity.HopDongId);
        return await GetByIdAsync(id);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, TrangThaiPhuLuc trangThai)
    {
        var entity = await _context.PhuLucHopDongs.FindAsync(id);
        if (entity == null) return false;

        entity.TrangThai = trangThai;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await RecalculateContractFromAddendumsAsync(entity.HopDongId);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.PhuLucHopDongs.FindAsync(id);
        if (entity == null) return false;

        var hopDongId = entity.HopDongId;
        _context.PhuLucHopDongs.Remove(entity);
        await _context.SaveChangesAsync();
        await RecalculateContractFromAddendumsAsync(hopDongId);
        return true;
    }

    private async Task RecalculateContractFromAddendumsAsync(Guid hopDongId)
    {
        var hopDong = await _context.HopDongs
            .Include(h => h.PhuLucHopDongs)
            .FirstOrDefaultAsync(h => h.Id == hopDongId);

        if (hopDong == null) return;

        // Sum effective adjustments from active/approved addendums
        var activeAddendums = hopDong.PhuLucHopDongs?
            .Where(p => p.TrangThai == TrangThaiPhuLuc.DaHieuLuc || p.TrangThai == TrangThaiPhuLuc.DaDuyet)
            .ToList() ?? new List<PhuLucHopDong>();

        var totalAdjustment = activeAddendums.Sum(p => p.GiaTriDieuChinh);
        
        // Expiration date from latest effective addendum
        var latestExpiryAddendum = activeAddendums
            .Where(p => p.ExpiredDateMoi.HasValue)
            .OrderByDescending(p => p.NgayHieuLuc ?? p.NgayKy ?? p.CreatedAt)
            .FirstOrDefault();

        if (latestExpiryAddendum?.ExpiredDateMoi != null)
        {
            hopDong.ExpiredDate = latestExpiryAddendum.ExpiredDateMoi;
        }

        hopDong.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static PhuLucHopDongDto MapToDto(PhuLucHopDong p)
    {
        return new PhuLucHopDongDto
        {
            Id = p.Id,
            HopDongId = p.HopDongId,
            SoPhuLuc = p.SoPhuLuc,
            TenPhuLuc = p.TenPhuLuc,
            LoaiPhuLuc = p.LoaiPhuLuc,
            TrangThai = p.TrangThai,
            NgayKy = p.NgayKy,
            NgayHieuLuc = p.NgayHieuLuc,
            GiaTriDieuChinh = p.GiaTriDieuChinh,
            ExpiredDateMoi = p.ExpiredDateMoi,
            ThoiHanThucHienMoi = p.ThoiHanThucHienMoi,
            NoiDungDieuChinh = p.NoiDungDieuChinh,
            GhiChu = p.GhiChu,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            DotThanhToans = p.DotThanhToans?.Select(d => new DotThanhToanDto
            {
                Id = d.Id,
                HopDongId = d.HopDongId,
                TenDot = d.TenDot,
                TyLeThanhToan = d.TyLeThanhToan,
                GiaTriThanhToan = d.GiaTriThanhToan,
                NgayThanhToan = d.NgayThanhToan,
                DieuKienThanhToan = d.DieuKienThanhToan,
                IsPaid = d.IsPaid,
                CreatedAt = d.CreatedAt,
                UpdatedAt = d.UpdatedAt
            }).ToList() ?? new(),
            HangHoaDichVus = p.HangHoaDichVus?.Select(h => new demo1.DTOs.HangHoaDichVu.HangHoaDichVuDto
            {
                Id = h.Id,
                IdParent = h.IdParent,
                Loai = h.Loai,
                Stt = h.Stt,
                DanhMucHangHoa = h.DanhMucHangHoa,
                TenDichVu = h.TenDichVu,
                KhoiLuong = h.KhoiLuong,
                DonGia = h.DonGia,
                ThanhTien = h.ThanhTien
            }).ToList() ?? new()
        };
    }
}
