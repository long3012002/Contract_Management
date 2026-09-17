using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Danh mục Loại hợp đồng.
/// </summary>
[Route("api/DanhMuc/loai-hop-dong")]
[FeatureAuthorize("DANH_MUC")]
public class LoaiHopDongsController : CrudControllerBase<LoaiHopDongDto, CreateLoaiHopDongDto, UpdateLoaiHopDongDto>
{
    public LoaiHopDongsController(ILoaiHopDongService service) : base(service)
    {
    }
}
