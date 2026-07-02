using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;

namespace demo1.Services.Implements;

public class ProjectService : InMemoryCrudService<Project, ProjectDto, CreateProjectDto, UpdateProjectDto>, IProjectService
{
    public ProjectService(IMapper mapper) : base(mapper)
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

    protected override Project CreateEntity(CreateProjectDto dto)
    {
        ProjectValidator.EnsureValidBudget(dto.TotalBudget);
        return base.CreateEntity(dto);
    }

    protected override void UpdateEntity(Project entity, UpdateProjectDto dto)
    {
        ProjectValidator.EnsureValidBudget(dto.TotalBudget);
        base.UpdateEntity(entity, dto);
    }
}
