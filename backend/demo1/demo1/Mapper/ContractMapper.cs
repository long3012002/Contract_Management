using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper;

public static class ContractMapper
{
    public static ContractDto ToDto(Contract entity)
    {
        return new ContractDto
        {
            Id = entity.Id,
            ContractNumber = entity.Code,
            Title = entity.Name,
            Description = entity.Description,
            ProjectId = entity.ProjectId,
            BidPackageId = entity.BidPackageId,
            ContractValue = entity.ContractValue,
            SignedDate = entity.SignedDate,
            EffectiveDate = entity.EffectiveDate,
            ExpiredDate = entity.ExpiredDate,
            RenewalReminderDate = entity.RenewalReminderDate,
            IsRenewalRequired = entity.IsRenewalRequired,
            Status = entity.Status,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Contract ToEntity(CreateContractDto dto)
    {
        return new Contract
        {
            Code = MapperHelpers.NormalizeCode(dto.ContractNumber),
            Name = MapperHelpers.TrimRequired(dto.Title),
            Description = MapperHelpers.TrimOptional(dto.Description),
            ProjectId = dto.ProjectId,
            BidPackageId = dto.BidPackageId,
            ContractValue = dto.ContractValue,
            SignedDate = dto.SignedDate,
            EffectiveDate = dto.EffectiveDate,
            ExpiredDate = dto.ExpiredDate,
            RenewalReminderDate = dto.RenewalReminderDate,
            IsRenewalRequired = dto.IsRenewalRequired,
            Status = MapperHelpers.TrimRequired(dto.Status)
        };
    }

    public static void ApplyUpdate(Contract entity, UpdateContractDto dto)
    {
        entity.Name = MapperHelpers.TrimRequired(dto.Title);
        entity.Description = MapperHelpers.TrimOptional(dto.Description);
        entity.ProjectId = dto.ProjectId;
        entity.BidPackageId = dto.BidPackageId;
        entity.ContractValue = dto.ContractValue;
        entity.SignedDate = dto.SignedDate;
        entity.EffectiveDate = dto.EffectiveDate;
        entity.ExpiredDate = dto.ExpiredDate;
        entity.RenewalReminderDate = dto.RenewalReminderDate;
        entity.IsRenewalRequired = dto.IsRenewalRequired;
        entity.Status = MapperHelpers.TrimRequired(dto.Status);
        entity.IsActive = dto.IsActive;
    }
}
