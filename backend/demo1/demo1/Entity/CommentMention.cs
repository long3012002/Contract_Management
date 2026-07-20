using System;

namespace demo1.Entity;

public class CommentMention
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CommentId { get; set; }
    public virtual CommentCongViecGoiThau Comment { get; set; } = null!;

    public Guid MentionedUserId { get; set; }
    public virtual User MentionedUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
