using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/doi-tac")]
[FeatureAuthorize("PARTNER")]
public class DoiTacsController : CrudControllerBase<DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>
{
    public DoiTacsController(IDoiTacService service) : base(service)
    {
    }
}
