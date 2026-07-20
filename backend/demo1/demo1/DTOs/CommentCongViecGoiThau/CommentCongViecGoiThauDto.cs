using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class CreateCommentCongViecGoiThauDto : IHasParentId
{
    // ParentId chính là idCongViec (CongViecGoiThauId)
    public Guid ParentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public List<Guid> MentionedUserIds { get; set; } = new();
}

public class UpdateCommentCongViecGoiThauDto
{
    public string Content { get; set; } = string.Empty;
    public List<Guid> MentionedUserIds { get; set; } = new();
}

public class CommentCongViecGoiThauDto : IHasId, IHasParentId
{
    public Guid Id { get; set; }
    // ParentId chính là idCongViec (CongViecGoiThauId)
    public Guid ParentId { get; set; }
    public Guid UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string UserUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<UserMentionDto> Mentions { get; set; } = new();
    public List<CommentCongViecGoiThauDto> Replies { get; set; } = new();
}

public class UserMentionDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
