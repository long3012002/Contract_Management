using demo1.DTOs;
using demo1.Entity;
using demo1.Mapper;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class PartnerService : InMemoryCrudService<Partner, PartnerDto, CreatePartnerDto, UpdatePartnerDto>, IPartnerService
{
    public PartnerService()
    {
        Seed(new Partner
        {
            Code = "NCC-001",
            Name = "Cong ty TNHH Minh Hoa",
            TaxCode = "0100000001",
            Phone = "0900000001",
            Email = "contact@minhhoa.example",
            Address = "Ha Noi"
        });
    }

    protected override PartnerDto ToDto(Partner entity) => PartnerMapper.ToDto(entity);

    protected override Partner CreateEntity(CreatePartnerDto dto)
    {
        PartnerValidator.EnsureValidEmail(dto.Email);
        return PartnerMapper.ToEntity(dto);
    }

    protected override void UpdateEntity(Partner entity, UpdatePartnerDto dto)
    {
        PartnerValidator.EnsureValidEmail(dto.Email);
        PartnerMapper.ApplyUpdate(entity, dto);
    }
}
