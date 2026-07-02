using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper;

public static class ProjectMapper
{
    public static ProjectDto ToDto(Project entity)
    {
        return new ProjectDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            TotalBudget = entity.TotalBudget,
            Status = entity.Status,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Project ToEntity(CreateProjectDto dto)
    {
        return new Project
        {
            Code = MapperHelpers.NormalizeCode(dto.Code),
            Name = MapperHelpers.TrimRequired(dto.Name),
            Description = MapperHelpers.TrimOptional(dto.Description),
            TotalBudget = dto.TotalBudget,
            Status = MapperHelpers.TrimRequired(dto.Status)
        };
    }

    public static void ApplyUpdate(Project entity, UpdateProjectDto dto)
    {
        entity.Name = MapperHelpers.TrimRequired(dto.Name);
        entity.Description = MapperHelpers.TrimOptional(dto.Description);
        entity.TotalBudget = dto.TotalBudget;
        entity.Status = MapperHelpers.TrimRequired(dto.Status);
        entity.IsActive = dto.IsActive;
    }
}
