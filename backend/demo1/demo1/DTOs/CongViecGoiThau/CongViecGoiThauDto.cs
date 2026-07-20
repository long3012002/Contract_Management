using System;

namespace demo1.DTOs;

public class CongViecGoiThauDto : IHasId, IHasParentId
{
    public Guid Id { get; set; }
    public Guid GoiThauId { get; set; }
    public Guid ParentId
    {
        get => GoiThauId;
        set => GoiThauId = value;
    }

    public int Stt { get; set; }
    public string TenTaiLieu { get; set; } = string.Empty;
    public DateTime? NgayKy { get; set; }
    public string? LoaiVanBan { get; set; }
    public string? TinhTrang { get; set; }
    public string? GhiChu { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCongViecGoiThauDto
{
    public Guid GoiThauId { get; set; }
    public int Stt { get; set; }
    public string TenTaiLieu { get; set; } = string.Empty;
    public DateTime? NgayKy { get; set; }
    public string? LoaiVanBan { get; set; }
    public string? TinhTrang { get; set; }
    public string? GhiChu { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateCongViecGoiThauDto
{
    public Guid? GoiThauId { get; set; }
    public int Stt { get; set; }
    public string TenTaiLieu { get; set; } = string.Empty;
    public DateTime? NgayKy { get; set; }
    public string? LoaiVanBan { get; set; }
    public string? TinhTrang { get; set; }
    public string? GhiChu { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class CongViecGoiThauReportDto
{
    public Guid GoiThauId { get; set; }
    public string TenGoiThau { get; set; } = string.Empty;
    public string MaGoiThau { get; set; } = string.Empty;
    public string? TenDuAn { get; set; }
    public decimal GiaTriGoiThau { get; set; }
    public List<CongViecGoiThauDto> CongViecs { get; set; } = new List<CongViecGoiThauDto>();
    public int TongSoCongViec { get; set; }
    public int SoCongViecDaHoanThanh { get; set; }
    public int SoCongViecDangThucHien { get; set; }
}
