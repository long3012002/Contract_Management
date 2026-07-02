using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/bid-packages")]
public class BidPackagesController : CrudControllerBase<BidPackageDto, CreateBidPackageDto, UpdateBidPackageDto>
{
    public BidPackagesController(IBidPackageService service) : base(service)
    {
    }
}
