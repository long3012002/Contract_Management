using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IProjectService : ICrudService<ProjectDto, CreateProjectDto, UpdateProjectDto>
{
}
