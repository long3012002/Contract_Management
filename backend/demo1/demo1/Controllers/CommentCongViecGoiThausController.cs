using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace demo1.Controllers;

[ApiController]
[Route("api/comment-cong-viec")]
[Authorize]
public class CommentCongViecGoiThausController : ControllerBase
{
    private readonly ICommentCongViecGoiThauService _commentService;

    public CommentCongViecGoiThausController(ICommentCongViecGoiThauService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("by-cong-viec/{idCongViec:guid}")]
    public async Task<ActionResult<IEnumerable<CommentCongViecGoiThauDto>>> GetByCongViecId(Guid idCongViec)
    {
        var comments = await _commentService.GetCommentsByCongViecIdAsync(idCongViec);
        return Ok(comments);
    }

    [HttpPost]
    public async Task<ActionResult<CommentCongViecGoiThauDto>> CreateComment([FromBody] CreateCommentCongViecGoiThauDto dto)
    {
        var created = await _commentService.CreateCommentAsync(dto);
        return Ok(created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentCongViecGoiThauDto>> UpdateComment(Guid id, [FromBody] UpdateCommentCongViecGoiThauDto dto)
    {
        var updated = await _commentService.UpdateCommentAsync(id, dto);
        return updated is null ? NotFound(new { message = "Không tìm thấy bình luận." }) : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var success = await _commentService.DeleteCommentAsync(id);
        return success ? NoContent() : NotFound(new { message = "Không tìm thấy bình luận." });
    }

    [HttpGet("mention-suggestions")]
    public async Task<ActionResult<IEnumerable<UserMentionDto>>> GetMentionSuggestions([FromQuery] string? search)
    {
        var suggestions = await _commentService.GetMentionSuggestionsAsync(search);
        return Ok(suggestions);
    }
}
