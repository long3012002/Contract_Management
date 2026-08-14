using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace demo1.Services.Implements;

public class WarningService : IWarningService
{
    private const int ExpiringSoonDays = 30;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WarningService> _logger;

    public WarningService(AppDbContext dbContext, ILogger<WarningService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<ContractWarningDto>> GetContractsExpiringSoonAsync()
    {
        try
        {
            var today = DateTime.Today;
            var thresholdDate = today.AddDays(ExpiringSoonDays);
            var result = await _dbContext.HopDongs
                .AsNoTracking()
                .Where(h => h.IsActive && h.ExpiredDate.HasValue && h.ExpiredDate.Value.Date >= today.Date && h.ExpiredDate.Value.Date <= thresholdDate.Date)
                .Select(h => new ContractWarningDto
                {
                    ContractId = h.Id,
                    ContractNumber = h.Code,
                    Title = h.Name,
                    ExpiredDate = h.ExpiredDate,
                    DaysRemaining = (h.ExpiredDate!.Value.Date - today).Days,
                    WarningMessage = $"Hợp đồng sắp hết hạn trong {(h.ExpiredDate!.Value.Date - today).Days} ngày."
                })
                .OrderBy(h => h.DaysRemaining)
                .ToListAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetContractsExpiringSoonAsync.");
            throw;
        }
    }

    public async Task<List<ContractWarningDto>> GetExpiredContractsAsync()
    {
        try
        {
            var today = DateTime.Today;

            var dbContracts = await _dbContext.HopDongs
                .AsNoTracking()
                .Where(c => c.IsActive 
                    && c.ExpiredDate.HasValue 
                    && c.ExpiredDate.Value.Date < today.Date)
                .Select(c => new { c.Id, c.Code, c.Name, c.ExpiredDate })
                .ToListAsync();

            return dbContracts
                .Select(c => new ContractWarningDto
                {
                    ContractId = c.Id,
                    ContractNumber = c.Code,
                    Title = c.Name,
                    ExpiredDate = c.ExpiredDate,
                    DaysRemaining = (c.ExpiredDate!.Value.Date - today).Days,
                    WarningMessage = "Hợp đồng đã hết hạn."
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetExpiredContractsAsync.");
            throw;
        }
    }

    public async Task<List<BudgetWarningDto>> GetOverBudgetContractsAsync()
    {
        try
        {
            var contractsWithTotals = await _dbContext.HopDongs
                .AsNoTracking()
                .Include(h => h.GoiThau)
                .Where(h => h.IsActive && h.GoiThauId.HasValue && h.GoiThau != null && h.GoiThau.GiaTriGoiThau > 0 && h.GiaTriHopDong > h.GoiThau.GiaTriGoiThau)
                .Select(h => new
                {
                    ContractId = h.Id,
                    Code = h.Code,
                    Name = h.Name,
                    GiaTriHopDong = h.GiaTriHopDong,
                    GiaGoiThau = h.GoiThau!.GiaTriGoiThau
                })
                .ToListAsync();

            var result = contractsWithTotals
                .Select(c => new BudgetWarningDto
                {
                    ContractId = c.ContractId,
                    ContractNumber = c.Code,
                    EstimatedValue = c.GiaGoiThau,
                    ContractValue = c.GiaTriHopDong,
                    OverValue = c.GiaTriHopDong - c.GiaGoiThau,
                    UsedPercent = Math.Round(c.GiaTriHopDong / c.GiaGoiThau * 100, 2),
                    WarningMessage = $"Hợp đồng '{c.Name}' (Mã: {c.Code}) vượt dự toán gói thầu {(c.GiaTriHopDong - c.GiaGoiThau):N0} VNĐ (Giá trị hợp đồng: {c.GiaTriHopDong:N0} VNĐ / Dự toán gói thầu: {c.GiaGoiThau:N0} VNĐ)."
                })
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetOverBudgetContractsAsync.");
            throw;
        }
    }

    public async Task<List<LicenseWarningDto>> GetLicensesExpiringSoonAsync()
    {
        try
        {
            var today = DateTime.Today;

            // Source 1: Licenses table
            var licenses = await _dbContext.Licenses
                .AsNoTracking()
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue)
                .ToListAsync();

            var resultList = licenses
                .Where(l =>
                {
                    var daysRemaining = (l.NgayKetThuc!.Value.Date - today).Days;
                    return daysRemaining <= l.CanhBaoTruocNgay && daysRemaining >= 0;
                })
                .Select(l => new LicenseWarningDto
                {
                    LicenseId = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    DuAnName = l.DuAn != null ? l.DuAn.Name : null,
                    HopDongName = l.HopDong != null ? l.HopDong.Name : null,
                    NgayKetThuc = l.NgayKetThuc,
                    DaysRemaining = (l.NgayKetThuc!.Value.Date - today).Days,
                    CanhBaoTruocNgay = l.CanhBaoTruocNgay,
                    WarningMessage = $"License sắp hết hạn trong {(l.NgayKetThuc!.Value.Date - today).Days} ngày."
                })
                .ToList();

            // Track linked license IDs to deduplicate
            var processedLicenseIds = new HashSet<Guid>(licenses.Select(l => l.Id));

            // Source 2: HangHoaDichVu table (Loai = License) unlinked
            var hangHoaLicenses = await _dbContext.HangHoaDichVus
                .AsNoTracking()
                .Where(h => h.IsActive && h.Loai == LoaiHangHoaDichVu.License && h.NgayKetThuc.HasValue && (h.IdLicense == null || !processedLicenseIds.Contains(h.IdLicense.Value)))
                .ToListAsync();

            if (hangHoaLicenses.Any())
            {
                var parentHopDongIds = hangHoaLicenses.Select(h => h.IdParent).Distinct().ToList();
                var parentHopDongs = await _dbContext.HopDongs
                    .AsNoTracking()
                    .Include(h => h.DuAn)
                    .Where(h => parentHopDongIds.Contains(h.Id))
                    .ToDictionaryAsync(h => h.Id);

                var hangHoaWarnings = hangHoaLicenses
                    .Where(h =>
                    {
                        var daysRemaining = (h.NgayKetThuc!.Value.Date - today).Days;
                        return daysRemaining <= ExpiringSoonDays && daysRemaining >= 0;
                    })
                    .Select(h =>
                    {
                        parentHopDongs.TryGetValue(h.IdParent, out var parentHd);
                        var licenseName = !string.IsNullOrWhiteSpace(h.TenDichVu)
                            ? h.TenDichVu
                            : (!string.IsNullOrWhiteSpace(h.DanhMucHangHoa) ? h.DanhMucHangHoa : h.KyMaHieu ?? "License Hợp đồng");
                        var daysRemaining = (h.NgayKetThuc!.Value.Date - today).Days;

                        return new LicenseWarningDto
                        {
                            LicenseId = h.Id,
                            Code = h.KyMaHieu ?? h.Stt ?? "HD-LIC",
                            Name = licenseName,
                            DuAnName = parentHd?.DuAn != null ? parentHd.DuAn.Name : null,
                            HopDongName = parentHd != null ? parentHd.Name : null,
                            NgayKetThuc = h.NgayKetThuc,
                            DaysRemaining = daysRemaining,
                            CanhBaoTruocNgay = ExpiringSoonDays,
                            WarningMessage = $"License Hợp đồng sắp hết hạn trong {daysRemaining} ngày."
                        };
                    });

                resultList.AddRange(hangHoaWarnings);
            }

            return resultList.OrderBy(l => l.DaysRemaining).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetLicensesExpiringSoonAsync.");
            throw;
        }
    }

    public async Task<List<LicenseWarningDto>> GetExpiredLicensesAsync()
    {
        try
        {
            var today = DateTime.Today;

            // Source 1: Licenses table
            var licenses = await _dbContext.Licenses
                .AsNoTracking()
                .Include(l => l.DuAn)
                .Include(l => l.HopDong)
                .Where(l => l.IsActive && l.LoaiLicense != 2 && l.NgayKetThuc.HasValue && l.NgayKetThuc.Value.Date < today.Date)
                .ToListAsync();

            var resultList = licenses
                .Select(l => new LicenseWarningDto
                {
                    LicenseId = l.Id,
                    Code = l.Code,
                    Name = l.Name,
                    DuAnName = l.DuAn != null ? l.DuAn.Name : null,
                    HopDongName = l.HopDong != null ? l.HopDong.Name : null,
                    NgayKetThuc = l.NgayKetThuc,
                    DaysRemaining = (l.NgayKetThuc!.Value.Date - today).Days,
                    CanhBaoTruocNgay = l.CanhBaoTruocNgay,
                    WarningMessage = "License đã quá hạn sử dụng."
                })
                .ToList();

            // Track linked license IDs to deduplicate
            var processedLicenseIds = new HashSet<Guid>(licenses.Select(l => l.Id));

            // Source 2: HangHoaDichVu table (Loai = License) unlinked expired
            var hangHoaLicenses = await _dbContext.HangHoaDichVus
                .AsNoTracking()
                .Where(h => h.IsActive && h.Loai == LoaiHangHoaDichVu.License && h.NgayKetThuc.HasValue && h.NgayKetThuc.Value.Date < today.Date && (h.IdLicense == null || !processedLicenseIds.Contains(h.IdLicense.Value)))
                .ToListAsync();

            if (hangHoaLicenses.Any())
            {
                var parentHopDongIds = hangHoaLicenses.Select(h => h.IdParent).Distinct().ToList();
                var parentHopDongs = await _dbContext.HopDongs
                    .AsNoTracking()
                    .Include(h => h.DuAn)
                    .Where(h => parentHopDongIds.Contains(h.Id))
                    .ToDictionaryAsync(h => h.Id);

                var hangHoaWarnings = hangHoaLicenses
                    .Select(h =>
                    {
                        parentHopDongs.TryGetValue(h.IdParent, out var parentHd);
                        var licenseName = !string.IsNullOrWhiteSpace(h.TenDichVu)
                            ? h.TenDichVu
                            : (!string.IsNullOrWhiteSpace(h.DanhMucHangHoa) ? h.DanhMucHangHoa : h.KyMaHieu ?? "License Hợp đồng");
                        var daysRemaining = (h.NgayKetThuc!.Value.Date - today).Days;

                        return new LicenseWarningDto
                        {
                            LicenseId = h.Id,
                            Code = h.KyMaHieu ?? h.Stt ?? "HD-LIC",
                            Name = licenseName,
                            DuAnName = parentHd?.DuAn != null ? parentHd.DuAn.Name : null,
                            HopDongName = parentHd != null ? parentHd.Name : null,
                            NgayKetThuc = h.NgayKetThuc,
                            DaysRemaining = daysRemaining,
                            CanhBaoTruocNgay = ExpiringSoonDays,
                            WarningMessage = "License Hợp đồng đã quá hạn sử dụng."
                        };
                    });

                resultList.AddRange(hangHoaWarnings);
            }

            return resultList.OrderBy(l => l.DaysRemaining).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra trong GetExpiredLicensesAsync.");
            throw;
        }
    }
}
