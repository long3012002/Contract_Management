using System;

namespace demo1.DTOs;

public interface IHasParentId
{
    Guid ParentId { get; set; }
}
