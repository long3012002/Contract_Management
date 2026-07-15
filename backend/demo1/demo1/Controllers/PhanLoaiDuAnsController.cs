using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/phan-loai-du-an")]
[FeatureAuthorize("PROJECT")]
public class PhanLoaiDuAnsController : CrudControllerBase<PhanLoaiDuAnDto, CreatePhanLoaiDuAnDto, UpdatePhanLoaiDuAnDto>
{
    public PhanLoaiDuAnsController(IPhanLoaiDuAnService service) : base(service)
    {
    }
}
