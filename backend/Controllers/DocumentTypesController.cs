using ContractManagement.Api.DTOs.DocumentTypes;
using ContractManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContractManagement.Api.Controllers;

[ApiController]
[Route("api/document-types")]
public class DocumentTypesController : ControllerBase
{
    private readonly DocumentTypeService _service;

    public DocumentTypesController(DocumentTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound("Không tìm thấy loại hồ sơ.") : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Mã và tên loại hồ sơ không được để trống.");

        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentTypeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Tên loại hồ sơ không được để trống.");

        var success = await _service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound("Không tìm thấy loại hồ sơ.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound("Không tìm thấy loại hồ sơ.");
    }
}
