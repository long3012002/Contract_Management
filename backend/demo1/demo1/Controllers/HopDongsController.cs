using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/hop-dong")]
[FeatureAuthorize("CONTRACT")]
public class HopDongsController : CrudControllerBase<HopDongDto, CreateHopDongDto, UpdateHopDongDto>
{
    public HopDongsController(IHopDongService service) : base(service)
    {
    }
}
