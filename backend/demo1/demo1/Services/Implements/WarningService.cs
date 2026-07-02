using demo1.DTOs;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class WarningService : IWarningService
{
    private const int ExpiringSoonDays = 30;
    private readonly IContractService _contractService;
    private readonly IBidPackageService _bidPackageService;

    public WarningService(IContractService contractService, IBidPackageService bidPackageService)
    {
        _contractService = contractService;
        _bidPackageService = bidPackageService;
    }

    public async Task<List<ContractWarningDto>> GetContractsExpiringSoonAsync()
    {
        var today = DateTime.Today;
        var contracts = await _contractService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive
                && contract.IsRenewalRequired
                && RenewalValidator.IsExpiringSoon(contract.ExpiredDate, today, ExpiringSoonDays))
            .Select(contract => ToContractWarning(contract, today, "Hop dong sap het han."))
            .ToList();
    }

    public async Task<List<ContractWarningDto>> GetExpiredContractsAsync()
    {
        var today = DateTime.Today;
        var contracts = await _contractService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive && RenewalValidator.IsExpired(contract.ExpiredDate, today))
            .Select(contract => ToContractWarning(contract, today, "Hop dong da het han."))
            .ToList();
    }

    public async Task<List<BudgetWarningDto>> GetOverBudgetContractsAsync()
    {
        var contracts = await _contractService.GetAllItemsAsync();
        var bidPackages = await _bidPackageService.GetAllItemsAsync();

        return contracts
            .Where(contract => contract.IsActive && contract.BidPackageId.HasValue)
            .Select(contract => new
            {
                Contract = contract,
                BidPackage = bidPackages.FirstOrDefault(item => item.Id == contract.BidPackageId.GetValueOrDefault())
            })
            .Where(item => item.BidPackage is not null
                && BudgetValidator.IsOverBudget(
                    item.Contract.ContractValue,
                    item.BidPackage.EstimatedValue,
                    item.BidPackage.WarningThresholdPercent))
            .Select(item => new BudgetWarningDto
            {
                ContractId = item.Contract.Id,
                ContractNumber = item.Contract.ContractNumber,
                EstimatedValue = item.BidPackage!.EstimatedValue,
                ContractValue = item.Contract.ContractValue,
                OverValue = item.Contract.ContractValue - item.BidPackage.EstimatedValue,
                UsedPercent = BudgetValidator.CalculateUsedPercent(
                    item.Contract.ContractValue,
                    item.BidPackage.EstimatedValue),
                WarningMessage = "Gia tri hop dong vuot nguong goi thau."
            })
            .ToList();
    }

    private static ContractWarningDto ToContractWarning(ContractDto contract, DateTime today, string message)
    {
        return new ContractWarningDto
        {
            ContractId = contract.Id,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            ExpiredDate = contract.ExpiredDate,
            DaysRemaining = contract.ExpiredDate.HasValue
                ? (contract.ExpiredDate.Value.Date - today.Date).Days
                : 0,
            WarningMessage = message
        };
    }
}
