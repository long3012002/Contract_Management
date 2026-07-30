using System;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// Controller cơ sở cung cấp các thao tác CRUD (Lấy danh sách phân trang, Chi tiết theo ID, Tạo mới, Cập nhật, Xóa) chuẩn hóa cho hệ thống.
/// </summary>
/// <typeparam name="TDto">Kiểu DTO hiển thị dữ liệu</typeparam>
/// <typeparam name="TCreateDto">Kiểu DTO tạo mới dữ liệu</typeparam>
/// <typeparam name="TUpdateDto">Kiểu DTO cập nhật dữ liệu</typeparam>
[Authorize]
[ApiController]
public abstract class CrudControllerBase<TDto, TCreateDto, TUpdateDto> : ControllerBase
    where TDto : IHasId
{
    private readonly ICrudService<TDto, TCreateDto, TUpdateDto> _service;

    protected CrudControllerBase(ICrudService<TDto, TCreateDto, TUpdateDto> service)
    {
        _service = service;
    }

    /// <summary>
    /// Lấy danh sách bản ghi có phân trang và hỗ trợ tìm kiếm/cursor.
    /// </summary>
    /// <param name="search">Từ khóa tìm kiếm (tùy chọn)</param>
    /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
    /// <param name="pageSize">Số lượng bản ghi mỗi trang (Mặc định: 20)</param>
    /// <param name="cursor">Con trỏ phân trang dạng cursor (tùy chọn)</param>
    /// <returns>Danh sách phân trang kèm tổng số bản ghi</returns>
    /// <response code="200">Lấy danh sách thành công</response>
    /// <response code="401">Chưa xác thực (Chưa truyền JWT Token)</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public virtual async Task<ActionResult<PagedResult<TDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cursor = null)
    {
        var result = await _service.GetAllAsync(search, page, pageSize, cursor);
        return Ok(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một bản ghi theo ID (GUID).
    /// </summary>
    /// <param name="id">Mã định danh duy nhất (GUID)</param>
    /// <returns>Thông tin chi tiết đối tượng</returns>
    /// <response code="200">Tìm thấy bản ghi</response>
    /// <response code="404">Không tìm thấy bản ghi theo ID</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<ActionResult<TDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Tạo mới một hoặc nhiều bản ghi.
    /// </summary>
    /// <param name="dtos">Danh sách dữ liệu cần thêm mới</param>
    /// <returns>Danh sách bản ghi sau khi tạo kèm ID</returns>
    /// <response code="200">Tạo mới thành công</response>
    /// <response code="400">Dữ liệu đầu vào không hợp lệ</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> Create([FromBody] IEnumerable<TCreateDto> dtos)
    {
        var result = await _service.CreateRangeAsync(dtos);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật thông tin bản ghi theo ID.
    /// </summary>
    /// <param name="id">Mã định danh duy nhất (GUID)</param>
    /// <param name="dto">Dữ liệu cập nhật</param>
    /// <response code="204">Cập nhật thành công (No Content)</response>
    /// <response code="404">Không tìm thấy bản ghi</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TUpdateDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Xóa bản ghi theo ID.
    /// </summary>
    /// <param name="id">Mã định danh duy nhất (GUID)</param>
    /// <response code="204">Xóa thành công (No Content)</response>
    /// <response code="404">Không tìm thấy bản ghi</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }
}
