using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

/// <summary>
/// API Quản lý Bình luận Công việc Gói thầu (Tạo bình luận, Cập nhật, Xóa, Lấy danh sách bình luận và Gợi ý Mention người dùng).
/// </summary>
[ApiController]
[Route("api/NghiepVu/comment-cong-viec")]
[Authorize]
public class CommentCongViecGoiThausController : ControllerBase
{
    private readonly ICommentCongViecGoiThauService _commentService;

    public CommentCongViecGoiThausController(ICommentCongViecGoiThauService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>
    /// Lấy danh sách bình luận gắn với một Bước Công việc Gói thầu.
    /// </summary>
    /// <param name="idCongViec">Mã định danh Bước công việc (GUID)</param>
    /// <returns>Danh sách bình luận</returns>
    /// <response code="200">Lấy bình luận thành công</response>
    [HttpGet("by-cong-viec/{idCongViec:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CommentCongViecGoiThauDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CommentCongViecGoiThauDto>>> GetByCongViecId(Guid idCongViec)
    {
        var comments = await _commentService.GetCommentsByCongViecIdAsync(idCongViec);
        return Ok(comments);
    }

    /// <summary>
    /// Thêm mới một bình luận vào Bước Công việc Gói thầu.
    /// </summary>
    /// <param name="dto">Nội dung bình luận và thông tin mention</param>
    /// <returns>Bình luận vừa tạo</returns>
    /// <response code="200">Tạo bình luận thành công</response>
    [HttpPost]
    [ProducesResponseType(typeof(CommentCongViecGoiThauDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CommentCongViecGoiThauDto>> CreateComment([FromBody] CreateCommentCongViecGoiThauDto dto)
    {
        var created = await _commentService.CreateCommentAsync(dto);
        return Ok(created);
    }

    /// <summary>
    /// Cập nhật nội dung một bình luận theo ID.
    /// </summary>
    /// <param name="id">Mã định danh Bình luận (GUID)</param>
    /// <param name="dto">Nội dung cập nhật</param>
    /// <returns>Bình luận sau khi sửa</returns>
    /// <response code="200">Cập nhật thành công</response>
    /// <response code="404">Không tìm thấy bình luận</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CommentCongViecGoiThauDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentCongViecGoiThauDto>> UpdateComment(Guid id, [FromBody] UpdateCommentCongViecGoiThauDto dto)
    {
        var updated = await _commentService.UpdateCommentAsync(id, dto);
        return updated is null ? NotFound(new { message = "Không tìm thấy bình luận." }) : Ok(updated);
    }

    /// <summary>
    /// Xóa một bình luận theo ID.
    /// </summary>
    /// <param name="id">Mã định danh Bình luận (GUID)</param>
    /// <response code="204">Xóa thành công (No Content)</response>
    /// <response code="404">Không tìm thấy bình luận</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var success = await _commentService.DeleteCommentAsync(id);
        return success ? NoContent() : NotFound(new { message = "Không tìm thấy bình luận." });
    }

    /// <summary>
    /// Lấy danh sách gợi ý Người dùng để Tag/Mention trong bình luận.
    /// </summary>
    /// <param name="search">Từ khóa tìm kiếm theo Tên/Username</param>
    /// <param name="page">Trang hiện tại (Mặc định: 1)</param>
    /// <param name="pageSize">Số bản ghi gợi ý (Mặc định: 6)</param>
    /// <returns>Danh sách gợi ý người dùng</returns>
    /// <response code="200">Lấy danh sách gợi ý thành công</response>
    [HttpGet("mention-suggestions")]
    [ProducesResponseType(typeof(IEnumerable<UserMentionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserMentionDto>>> GetMentionSuggestions([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 6)
    {
        var suggestions = await _commentService.GetMentionSuggestionsAsync(search, page, pageSize);
        return Ok(suggestions);
    }
}
