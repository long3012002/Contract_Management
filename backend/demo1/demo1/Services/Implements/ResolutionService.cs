using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;

namespace demo1.Services.Implements;

public class ResolutionService : InMemoryCrudService<Resolution, ResolutionDto, CreateResolutionDto, UpdateResolutionDto>, IResolutionService
{
    public ResolutionService(IMapper mapper) : base(mapper)
    {
        Seed(new Resolution
        {
            Code = "NQ-001",
            Name = "Nghi quyet phe duyet chu truong",
            Description = "Ho so mau cho giai doan 1",
            IssuedDate = DateTime.Today.AddDays(-30),
            EffectiveDate = DateTime.Today.AddDays(-25)
        });
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
