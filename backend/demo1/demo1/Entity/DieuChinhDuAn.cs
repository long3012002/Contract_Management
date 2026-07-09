using System;

namespace demo1.Entity;

public class DieuChinhDuAn : BaseEntity
{
    public Guid DuAnId { get; set; }
    public virtual DuAn DuAn { get; set; } = null!;
    
    public decimal GiaTriDieuChinh { get; set; }
    public string LyDoDieuChinh { get; set; } = string.Empty;
    public DateTime NgayDieuChinh { get; set; } = DateTime.UtcNow;
}
