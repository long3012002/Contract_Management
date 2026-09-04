using System;

namespace demo1.Entity;

public class DuAnNguonTrienKhai
{
    public Guid TrienKhaiProjectId { get; set; }
    public virtual DuAn TrienKhaiProject { get; set; } = null!;

    public Guid NguonProjectId { get; set; }
    public virtual DuAn NguonProject { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
