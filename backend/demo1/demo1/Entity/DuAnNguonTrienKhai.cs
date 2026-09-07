using System;

namespace demo1.Entity;

public class DuAnNguonTrienKhai
{
    public Guid TrienKhaiProjectId { get; set; }
    public virtual DuAn TrienKhaiProject { get; set; } = null!;

    public string? NguonProjectId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
