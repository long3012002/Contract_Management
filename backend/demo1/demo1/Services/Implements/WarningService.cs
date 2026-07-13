using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class WarningService : IWarningService
{
    private const int ExpiringSoonDays = 30;
    private readonly IHopDongService _hopDongService;
    private readonly IGoiThauService _goiThauService;

    public WarningService(IHopDongService hopDongService, IGoiThauService goiThauService)
    {
        _hopDongService = hopDongService;
        _goiThauService = goiThauService;
    }

    public async Task<List<ContractWarningDto>> GetContractsExpiringSoonAsync()
    {
        var today = DateTime.Today;
        var contracts = await _hopDongService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive
                && contract.IsRenewalRequired
                && RenewalValidator.IsExpiringSoon(contract.ExpiredDate, today, ExpiringSoonDays))
            .Select(contract => ToContractWarning(contract, today, "Hợp đồng sắp hết hạn."))
            .ToList();
    }

    public async Task<List<ContractWarningDto>> GetExpiredContractsAsync()
    {
        var today = DateTime.Today;
        var contracts = await _hopDongService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive && RenewalValidator.IsExpired(contract.ExpiredDate, today))
            .Select(contract => ToContractWarning(contract, today, "Hợp đồng đã hết hạn."))
            .ToList();
    }

    public async Task<List<BudgetWarningDto>> GetOverBudgetContractsAsync()
    {
        var contracts = await _hopDongService.GetAllItemsAsync();
        var goiThaus = await _goiThauService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive && contract.GoiThauId.HasValue)
            .Select(contract => new
            {
                Contract = contract,
                GoiThau = goiThaus.FirstOrDefault(item => item.Id == contract.GoiThauId.GetValueOrDefault())
            })
            .Where(item => item.GoiThau is not null
                && BudgetValidator.IsOverBudget(
                    item.Contract.GiaTriHopDong,
                    item.GoiThau.GiaTriGoiThau,
                    item.GoiThau.NguongCanhBaoPercent))
            .Select(item => new BudgetWarningDto
            {
                ContractId = item.Contract.Id,
                ContractNumber = item.Contract.Code,
                EstimatedValue = item.GoiThau!.GiaTriGoiThau,
                ContractValue = item.Contract.GiaTriHopDong,
                OverValue = item.Contract.GiaTriHopDong - item.GoiThau.GiaTriGoiThau,
                UsedPercent = BudgetValidator.CalculateUsedPercent(
                    item.Contract.GiaTriHopDong,
                    item.GoiThau.GiaTriGoiThau),
                WarningMessage = "Giá trị hợp đồng vượt ngưỡng gói thầu."
            })
            .ToList();
    }

    private static ContractWarningDto ToContractWarning(HopDongDto contract, DateTime today, string message)
    {
        return new ContractWarningDto
        {
            ContractId = contract.Id,
            ContractNumber = contract.Code,
            Title = contract.Name,
            ExpiredDate = contract.ExpiredDate,
            DaysRemaining = contract.ExpiredDate.HasValue
                ? (contract.ExpiredDate.Value.Date - today.Date).Days
                : 0,
            WarningMessage = message
        };
    }
}
