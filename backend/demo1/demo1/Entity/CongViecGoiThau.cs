using System;
using System.ComponentModel.DataAnnotations.Schema;
using demo1.DTOs;

namespace demo1.Entity;

public class CongViecGoiThau : BaseEntity, IHasParentId
{
    public Guid GoiThauId { get; set; }
    public virtual GoiThau? GoiThau { get; set; }

    public int Stt { get; set; }
    public string TenTaiLieu { get; set; } = string.Empty;
    public DateTime? NgayKy { get; set; }
    public string? LoaiVanBan { get; set; }
    public string? TinhTrang { get; set; }
    public string? GhiChu { get; set; }

    public virtual ICollection<CongViecNguoiLienQuan> NguoiLienQuans { get; set; } = new List<CongViecNguoiLienQuan>();

    public Guid? CreateUserId { get; set; }
    public virtual User? CreateUser { get; set; }

    public Guid? ModifiedUserId { get; set; }
    public virtual User? ModifiedUser { get; set; }

    [NotMapped]
    public Guid ParentId
    {
        get => GoiThauId;
        set => GoiThauId = value;
    }
}
