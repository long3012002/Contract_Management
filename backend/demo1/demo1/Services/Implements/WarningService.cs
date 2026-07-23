using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class WarningService : IWarningService
{
    private const int ExpiringSoonDays = 30;
    private readonly AppDbContext _dbContext;

    public WarningService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ContractWarningDto>> GetContractsExpiringSoonAsync()
    {
        var today = DateTime.Today;
        var thresholdDate = today.AddDays(ExpiringSoonDays);

        var dbContracts = await _dbContext.HopDongs
            .AsNoTracking()
            .Where(c => c.IsActive 
                && c.IsRenewalRequired 
                && c.ExpiredDate.HasValue 
                && c.ExpiredDate.Value.Date >= today.Date 
                && c.ExpiredDate.Value.Date <= thresholdDate.Date)
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
                WarningMessage = "Hợp đồng sắp hết hạn."
            })
            .ToList();
    }

    public async Task<List<ContractWarningDto>> GetExpiredContractsAsync()
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

    public async Task<List<BudgetWarningDto>> GetOverBudgetContractsAsync()
    {
        var dbData = await _dbContext.HopDongs
            .AsNoTracking()
            .Where(h => h.IsActive && h.GoiThauId.HasValue)
            .Join(_dbContext.GoiThaus.AsNoTracking(),
                h => h.GoiThauId,
                gt => gt.Id,
                (h, gt) => new { Contract = h, GoiThau = gt })
            .Where(x => x.GoiThau.GiaTriGoiThau > 0 
                && x.Contract.GiaTriHopDong > (x.GoiThau.GiaTriGoiThau * x.GoiThau.NguongCanhBaoPercent / 100))
            .Select(x => new 
            {
                ContractId = x.Contract.Id,
                ContractNumber = x.Contract.Code,
                EstimatedValue = x.GoiThau.GiaTriGoiThau,
                ContractValue = x.Contract.GiaTriHopDong
            })
            .ToListAsync();

        return dbData
            .Select(x => new BudgetWarningDto
            {
                ContractId = x.ContractId,
                ContractNumber = x.ContractNumber,
                EstimatedValue = x.EstimatedValue,
                ContractValue = x.ContractValue,
                OverValue = x.ContractValue - x.EstimatedValue,
                UsedPercent = Math.Round(x.ContractValue / x.EstimatedValue * 100, 2),
                WarningMessage = "Giá trị hợp đồng vượt ngưỡng gói thầu."
            })
            .ToList();
    }
}
