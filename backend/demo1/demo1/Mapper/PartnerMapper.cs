using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper;

public static class PartnerMapper
{
    public static PartnerDto ToDto(Partner entity)
    {
        return new PartnerDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            TaxCode = entity.TaxCode,
            Phone = entity.Phone,
            Email = entity.Email,
            Address = entity.Address,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Partner ToEntity(CreatePartnerDto dto)
    {
        return new Partner
        {
            Code = MapperHelpers.NormalizeCode(dto.Code),
            Name = MapperHelpers.TrimRequired(dto.Name),
            Description = MapperHelpers.TrimOptional(dto.Description),
            TaxCode = MapperHelpers.TrimOptional(dto.TaxCode),
            Phone = MapperHelpers.TrimOptional(dto.Phone),
            Email = MapperHelpers.TrimOptional(dto.Email),
            Address = MapperHelpers.TrimOptional(dto.Address)
        };
    }

    public static void ApplyUpdate(Partner entity, UpdatePartnerDto dto)
    {
        entity.Name = MapperHelpers.TrimRequired(dto.Name);
        entity.Description = MapperHelpers.TrimOptional(dto.Description);
        entity.TaxCode = MapperHelpers.TrimOptional(dto.TaxCode);
        entity.Phone = MapperHelpers.TrimOptional(dto.Phone);
        entity.Email = MapperHelpers.TrimOptional(dto.Email);
        entity.Address = MapperHelpers.TrimOptional(dto.Address);
        entity.IsActive = dto.IsActive;
    }
}
