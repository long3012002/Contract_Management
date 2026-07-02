using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class ResolutionService : DbCrudService<Resolution, ResolutionDto, CreateResolutionDto, UpdateResolutionDto>, IResolutionService
{
    public ResolutionService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    protected override Resolution CreateEntity(CreateResolutionDto dto)
    {
        ResolutionValidator.EnsureValidDates(dto.IssuedDate, dto.EffectiveDate);
        return base.CreateEntity(dto);
    }

    protected override void UpdateEntity(Resolution entity, UpdateResolutionDto dto)
    {
        ResolutionValidator.EnsureValidDates(dto.IssuedDate, dto.EffectiveDate);
        base.UpdateEntity(entity, dto);
    }
}
