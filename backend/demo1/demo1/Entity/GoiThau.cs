using System;
using System.Collections.Generic;

namespace demo1.Entity;

public class GoiThau : BaseEntity
{
    public Guid? DuAnId { get; set; }
    public virtual DuAn? DuAn { get; set; }
    
    public decimal GiaTriGoiThau { get; set; }

    public virtual ICollection<CongViecGoiThau> CongViecGoiThaus { get; set; } = new List<CongViecGoiThau>();
}

