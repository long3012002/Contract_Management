using System;
using System.Collections.Generic;

namespace demo1.Entity;

public class GoiThau : BaseEntity
{
    public Guid? DuAnId { get; set; }
    public virtual DuAn? DuAn { get; set; }
    
    public decimal GiaTriGoiThau { get; set; }
    public decimal NguongCanhBaoPercent { get; set; } = 100;

    public virtual ICollection<NhaThauGoiThau> NhaThauGoiThaus { get; set; } = new List<NhaThauGoiThau>();
    public virtual ICollection<CongViecGoiThau> CongViecGoiThaus { get; set; } = new List<CongViecGoiThau>();
}

