using demo1.DTOs;
using demo1.Entity;
using demo1.Mapper;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class ResolutionService : InMemoryCrudService<Resolution, ResolutionDto, CreateResolutionDto, UpdateResolutionDto>, IResolutionService
{
    public ResolutionService()
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

    protected override ResolutionDto ToDto(Resolution entity) => ResolutionMapper.ToDto(entity);

    protected override Resolution CreateEntity(CreateResolutionDto dto)
    {
        ResolutionValidator.EnsureValidDates(dto.IssuedDate, dto.EffectiveDate);
        return ResolutionMapper.ToEntity(dto);
    }

    protected override void UpdateEntity(Resolution entity, UpdateResolutionDto dto)
    {
        ResolutionValidator.EnsureValidDates(dto.IssuedDate, dto.EffectiveDate);
        ResolutionMapper.ApplyUpdate(entity, dto);
    }
}
