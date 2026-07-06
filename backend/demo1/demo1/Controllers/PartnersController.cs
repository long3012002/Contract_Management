using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/partners")]
[FeatureAuthorize("PARTNER")]
public class PartnersController : CrudControllerBase<PartnerDto, CreatePartnerDto, UpdatePartnerDto>
{
    public PartnersController(IPartnerService service) : base(service)
    {
    }
}
