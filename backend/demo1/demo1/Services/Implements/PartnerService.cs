using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class PartnerService : DbCrudService<Partner, PartnerDto, CreatePartnerDto, UpdatePartnerDto>, IPartnerService
{
    public PartnerService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    protected override Partner CreateEntity(CreatePartnerDto dto)
    {
        PartnerValidator.EnsureValidEmail(dto.Email);
        return base.CreateEntity(dto);
    }

    protected override void UpdateEntity(Partner entity, UpdatePartnerDto dto)
    {
        PartnerValidator.EnsureValidEmail(dto.Email);
        base.UpdateEntity(entity, dto);
    }
}
