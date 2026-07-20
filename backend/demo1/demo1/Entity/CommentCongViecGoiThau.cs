using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using demo1.DTOs;

namespace demo1.Entity;

public class CommentCongViecGoiThau : BaseEntity, IHasParentId
{
    public Guid CongViecGoiThauId { get; set; }
    public virtual CongViecGoiThau CongViecGoiThau { get; set; } = null!;

    [NotMapped]
    public Guid ParentId
    {
        get => CongViecGoiThauId;
        set => CongViecGoiThauId = value;
    }

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Content { get; set; } = string.Empty;

    public Guid? ParentCommentId { get; set; }
    public virtual CommentCongViecGoiThau? ParentComment { get; set; }
    public virtual ICollection<CommentCongViecGoiThau> Replies { get; set; } = new List<CommentCongViecGoiThau>();

    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public virtual ICollection<CommentMention> Mentions { get; set; } = new List<CommentMention>();
}
