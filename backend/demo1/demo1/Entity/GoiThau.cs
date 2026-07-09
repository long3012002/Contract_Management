using System;

namespace demo1.Entity;

public class GoiThau : BaseEntity
{
    public Guid? DuAnId { get; set; }
    public virtual DuAn? DuAn { get; set; }
    
    public decimal GiaTriGoiThau { get; set; }
    public decimal NguongCanhBaoPercent { get; set; } = 100;
}
