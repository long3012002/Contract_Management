using System;
using System.Threading.Tasks;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[ApiController]
[Route("api/HeThong/system-code")]
[Authorize]
public class SystemCodeController : ControllerBase
{
    private readonly ICodeGeneratorService _codeGeneratorService;

    public SystemCodeController(ICodeGeneratorService codeGeneratorService)
    {
        _codeGeneratorService = codeGeneratorService;
    }

    [HttpGet("generate/du-an")]
    public async Task<IActionResult> GenerateDuAnCode([FromQuery] int? nam = null)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateDuAnCodeAsync(nam);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/goi-thau")]
    public async Task<IActionResult> GenerateGoiThauCode([FromQuery] int? nam = null)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateGoiThauCodeAsync(nam);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/hop-dong")]
    public async Task<IActionResult> GenerateHopDongCode([FromQuery] int? nam = null)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateHopDongCodeAsync(nam);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/dot-thanh-toan")]
    public async Task<IActionResult> GenerateDotThanhToanCode([FromQuery] Guid hopDongId, [FromQuery] int? nam = null)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateDotThanhToanCodeAsync(hopDongId, nam);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/doi-tac")]
    public async Task<IActionResult> GenerateDoiTacCode([FromQuery] string name)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateDoiTacCodeAsync(name);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/nguon-von")]
    public async Task<IActionResult> GenerateNguonVonCode([FromQuery] string name)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateNguonVonCodeAsync(name);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/phan-loai-du-an")]
    public async Task<IActionResult> GeneratePhanLoaiDuAnCode([FromQuery] string name)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GeneratePhanLoaiDuAnCodeAsync(name);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpGet("generate/loai-hop-dong")]
    public async Task<IActionResult> GenerateLoaiHopDongCode([FromQuery] string name)
    {
        // Tạm comment code tự sinh mã để cho phép người dùng tự nhập
        // var code = await _codeGeneratorService.GenerateLoaiHopDongCodeAsync(name);
        // return Ok(new { code });
        return Ok(new { code = "" });
    }

    [HttpPost("migrate-legacy-codes")]
    public async Task<IActionResult> MigrateLegacyCodes()
    {
        var migratedCount = await _codeGeneratorService.MigrateAllLegacyCodesAsync();
        return Ok(new { message = "Migrated legacy codes successfully", migratedRecords = migratedCount });
    }
}
