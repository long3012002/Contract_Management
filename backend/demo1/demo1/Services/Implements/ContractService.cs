using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class ContractService : DbCrudService<Contract, ContractDto, CreateContractDto, UpdateContractDto>, IContractService
{
    public ContractService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
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
