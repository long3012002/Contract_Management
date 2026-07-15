using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/nhom-du-an")]
[FeatureAuthorize("PROJECT")]
public class NhomDuAnsController : CrudControllerBase<NhomDuAnDto, CreateNhomDuAnDto, UpdateNhomDuAnDto>
{
    public NhomDuAnsController(INhomDuAnService service) : base(service)
    {
    }
}
