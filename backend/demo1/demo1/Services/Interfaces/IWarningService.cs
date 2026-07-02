using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IWarningService
{
    Task<List<ContractWarningDto>> GetContractsExpiringSoonAsync();
    Task<List<ContractWarningDto>> GetExpiredContractsAsync();
    Task<List<BudgetWarningDto>> GetOverBudgetContractsAsync();
}
