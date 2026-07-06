using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/projects")]
[FeatureAuthorize("PROJECT")]
public class ProjectsController : CrudControllerBase<ProjectDto, CreateProjectDto, UpdateProjectDto>
{
    public ProjectsController(IProjectService service) : base(service)
    {
    }
}
