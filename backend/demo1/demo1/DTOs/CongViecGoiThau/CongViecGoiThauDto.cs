using System;
using System.Collections.Generic;

namespace demo1.DTOs;

public class CongViecNguoiLienQuanDto
{
    public Guid Id { get; set; }
    public Guid CongViecGoiThauId { get; set; }
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string TrangThaiXacNhan { get; set; } = "Pending";
    public DateTime HanXacNhanAt { get; set; }
    public DateTime? XacNhanAt { get; set; }
    public string? LoaiXacNhan { get; set; }
    public double SoGioConLai { get; set; }
    public bool IsOverdue { get; set; }
    /// <summary>Tên phòng ban của người liên quan, lấy trực tiếp từ User entity.</summary>
    public string? TenPhongBan { get; set; }
    /// <summary>Tên đơn vị của người liên quan, lấy trực tiếp từ User entity.</summary>
    public string? TenDonVi { get; set; }
}

public class ForwardStakeholdersRequestDto
{
    public List<Guid> UserIds { get; set; } = new List<Guid>();
    public string? GhiChu { get; set; }
}

public class CongViecLichSuChuyenTiepDto
{
    public Guid Id { get; set; }
    public Guid CongViecGoiThauId { get; set; }
    public int Step { get; set; }
    public string LoaiHanhDong { get; set; } = "Forward";
    public Guid FromUserId { get; set; }
    public string? FromUserFullName { get; set; }
    public string? FromUsername { get; set; }
    public Guid ToUserId { get; set; }
    public string? ToUserFullName { get; set; }
    public string? ToUsername { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
}

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

    public List<Guid> NguoiLienQuanIds { get; set; } = new List<Guid>();
    public List<CongViecNguoiLienQuanDto> NguoiLienQuans { get; set; } = new List<CongViecNguoiLienQuanDto>();
    public List<CongViecLichSuChuyenTiepDto> LichSuChuyenTieps { get; set; } = new List<CongViecLichSuChuyenTiepDto>();
    public List<FileAttachmentDto> FileAttachments { get; set; } = new List<FileAttachmentDto>();

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Guid? CreateUserId { get; set; }
    public string? CreateUserFullName { get; set; }
    public Guid? ModifiedUserId { get; set; }
    public string? ModifiedUserFullName { get; set; }

    /// <summary>
    /// Số lượng bình luận của công việc gói thầu
    /// </summary>
    public int SoBinhLuan { get; set; }
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

    public List<Guid>? NguoiLienQuanIds { get; set; }

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

    public List<Guid>? NguoiLienQuanIds { get; set; }

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
