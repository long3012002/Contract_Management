using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[Route("api/hop-dong")]
[FeatureAuthorize("CONTRACT_MANAGEMENT")]
public class HopDongsController : CrudControllerBase<HopDongDto, CreateHopDongDto, UpdateHopDongDto>
{
    private readonly IHopDongService _hopDongService;

    public HopDongsController(IHopDongService service) : base(service)
    {
        _hopDongService = service;
    }

    [HttpPut("dot-thanh-toan/{dotThanhToanId:guid}/pay")]
    public async Task<IActionResult> ConfirmPayment(Guid dotThanhToanId)
    {
        var success = await _hopDongService.ConfirmPaymentAsync(dotThanhToanId);
        return success 
            ? Ok(new { message = "Xác nhận thanh toán thành công." }) 
            : NotFound(new { message = "Không tìm thấy đợt thanh toán." });
    }
}
