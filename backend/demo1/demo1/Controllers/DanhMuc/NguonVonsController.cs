using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Nguồn vốn (vd: Chi phí của NHHT, Chi phí tại chi nhánh, Nguồn khác, Quỹ phúc lợi...).
/// </summary>
[Route("api/DanhMuc/nguon-von")]
[FeatureAuthorize("DU_AN")]
public class NguonVonsController : CrudControllerBase<NguonVonDto, CreateNguonVonDto, UpdateNguonVonDto>
{
    public NguonVonsController(INguonVonService service) : base(service)
    {
    }
}
