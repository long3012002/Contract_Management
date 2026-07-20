using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ICommentCongViecGoiThauService
{
    Task<IEnumerable<CommentCongViecGoiThauDto>> GetCommentsByCongViecIdAsync(Guid idCongViec);
    Task<CommentCongViecGoiThauDto> CreateCommentAsync(CreateCommentCongViecGoiThauDto dto);
    Task<CommentCongViecGoiThauDto?> UpdateCommentAsync(Guid id, UpdateCommentCongViecGoiThauDto dto);
    Task<bool> DeleteCommentAsync(Guid id);
    Task<IEnumerable<UserMentionDto>> GetMentionSuggestionsAsync(string? search);
}
