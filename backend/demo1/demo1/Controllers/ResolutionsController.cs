using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/resolutions")]
public class ResolutionsController : CrudControllerBase<ResolutionDto, CreateResolutionDto, UpdateResolutionDto>
{
    public ResolutionsController(IResolutionService service) : base(service)
    {
    }
}
