using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper;

public static class ResolutionMapper
{
    public static ResolutionDto ToDto(Resolution entity)
    {
        return new ResolutionDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Title = entity.Name,
            Summary = entity.Description,
            IssuedDate = entity.IssuedDate,
            EffectiveDate = entity.EffectiveDate,
            FileUrl = entity.FileUrl,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Resolution ToEntity(CreateResolutionDto dto)
    {
        return new Resolution
        {
            Code = MapperHelpers.NormalizeCode(dto.Code),
            Name = MapperHelpers.TrimRequired(dto.Title),
            Description = MapperHelpers.TrimOptional(dto.Summary),
            IssuedDate = dto.IssuedDate,
            EffectiveDate = dto.EffectiveDate,
            FileUrl = MapperHelpers.TrimOptional(dto.FileUrl)
        };
    }

    public static void ApplyUpdate(Resolution entity, UpdateResolutionDto dto)
    {
        entity.Name = MapperHelpers.TrimRequired(dto.Title);
        entity.Description = MapperHelpers.TrimOptional(dto.Summary);
        entity.IssuedDate = dto.IssuedDate;
        entity.EffectiveDate = dto.EffectiveDate;
        entity.FileUrl = MapperHelpers.TrimOptional(dto.FileUrl);
        entity.IsActive = dto.IsActive;
    }
}
