using demo1.DTOs;
using demo1.Entity;
using demo1.Mapper;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class ProjectService : InMemoryCrudService<Project, ProjectDto, CreateProjectDto, UpdateProjectDto>, IProjectService
{
    public ProjectService()
    {
        Seed(new Project
        {
            Code = "PRJ-001",
            Name = "Du an quan ly hop dong",
            Description = "Du an mau cho giai doan 1",
            TotalBudget = 1_000_000_000,
            Status = "Active"
        });
    }

    protected override ProjectDto ToDto(Project entity) => ProjectMapper.ToDto(entity);

    protected override Project CreateEntity(CreateProjectDto dto)
    {
        ProjectValidator.EnsureValidBudget(dto.TotalBudget);
        return ProjectMapper.ToEntity(dto);
    }

    protected override void UpdateEntity(Project entity, UpdateProjectDto dto)
    {
        ProjectValidator.EnsureValidBudget(dto.TotalBudget);
        ProjectMapper.ApplyUpdate(entity, dto);
    }
}
