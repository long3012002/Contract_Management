using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class ProjectService : DbCrudService<Project, ProjectDto, CreateProjectDto, UpdateProjectDto>, IProjectService
{
    public ProjectService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
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
