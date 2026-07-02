using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;

namespace demo1.Services.Implements;

public class ContractService : InMemoryCrudService<Contract, ContractDto, CreateContractDto, UpdateContractDto>, IContractService
{
    public ContractService(IMapper mapper) : base(mapper)
    {
        Seed(
            new Contract
            {
                Code = "HD-001",
                Name = "Hop dong trien khai he thong",
                ProjectId = 1,
                BidPackageId = 1,
                ContractValue = 750_000_000,
                SignedDate = DateTime.Today.AddDays(-20),
                EffectiveDate = DateTime.Today.AddDays(-15),
                ExpiredDate = DateTime.Today.AddDays(20),
                RenewalReminderDate = DateTime.Today.AddDays(10),
                IsRenewalRequired = true,
                Status = "Active"
            },
            new Contract
            {
                Code = "HD-002",
                Name = "Hop dong sap het hieu luc",
                ProjectId = 1,
                BidPackageId = 1,
                ContractValue = 850_000_000,
                SignedDate = DateTime.Today.AddDays(-90),
                EffectiveDate = DateTime.Today.AddDays(-85),
                ExpiredDate = DateTime.Today.AddDays(-2),
                RenewalReminderDate = DateTime.Today.AddDays(-10),
                IsRenewalRequired = true,
                Status = "Expired"
            });
    }

    protected override Contract CreateEntity(CreateContractDto dto)
    {
        ContractValidator.EnsureValid(dto.ContractValue, dto.EffectiveDate, dto.ExpiredDate);
        return base.CreateEntity(dto);
    }

    protected override void UpdateEntity(Contract entity, UpdateContractDto dto)
    {
        ContractValidator.EnsureValid(dto.ContractValue, dto.EffectiveDate, dto.ExpiredDate);
        base.UpdateEntity(entity, dto);
    }
}
