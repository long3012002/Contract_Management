using AutoMapper;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // DuAn mappings
            CreateMap<DuAn, DuAnDto>()
                .ForMember(dest => dest.TongDuToanHienTai, opt => opt.MapFrom(src => 
                    src.DuToanPheDuyet + (src.DieuChinhs != null ? src.DieuChinhs.Sum(dc => dc.GiaTriDieuChinh) : 0)))
                .ForMember(dest => dest.NhomDuAnName, opt => opt.MapFrom(src => src.NhomDuAn != null ? src.NhomDuAn.Name : null))
                .ForMember(dest => dest.PhanLoaiDuAnName, opt => opt.MapFrom(src => src.PhanLoaiDuAn != null ? src.PhanLoaiDuAn.Name : null));
            CreateMap<CreateDuAnDto, DuAn>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ChuDauTu, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ChuDauTu)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.ThoiGianThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiGianThucHien)))
                .ForMember(dest => dest.NoiDung, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.NoiDung)))
                .ForMember(dest => dest.HinhThucQuanLy, opt => opt.MapFrom(src => src.HinhThucQuanLy))
                .ForMember(dest => dest.ToChucThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ToChucThucHien)))
                .ForMember(dest => dest.NguonDuAnIds, opt => opt.Ignore()); // Will be set in service
            CreateMap<UpdateDuAnDto, DuAn>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ChuDauTu, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ChuDauTu)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.ThoiGianThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiGianThucHien)))
                .ForMember(dest => dest.NoiDung, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.NoiDung)))
                .ForMember(dest => dest.HinhThucQuanLy, opt => opt.MapFrom(src => src.HinhThucQuanLy))
                .ForMember(dest => dest.ToChucThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ToChucThucHien)));

            // DieuChinhDuAn mappings
            CreateMap<DieuChinhDuAn, DieuChinhDuAnDto>();

            // GoiThau mappings
            CreateMap<GoiThau, GoiThauDto>()
                .ForMember(dest => dest.DuAnName, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Name : null));
            CreateMap<CreateGoiThauDto, GoiThau>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdateGoiThauDto, GoiThau>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));

            // DoiTac mappings
            CreateMap<DoiTac, DoiTacDto>();
            CreateMap<CreateDoiTacDto, DoiTac>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.TaxCode)))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Phone)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Email)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Address)))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Account)))
                .ForMember(dest => dest.Representative, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Representative)))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Position)));
            CreateMap<UpdateDoiTacDto, DoiTac>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.TaxCode)))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Phone)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Email)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Address)))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Account)))
                .ForMember(dest => dest.Representative, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Representative)))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Position)));

            // Resolution mappings
            CreateMap<Resolution, ResolutionDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Summary, opt => opt.MapFrom(src => src.Description));
            CreateMap<CreateResolutionDto, Resolution>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Title)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Summary)))
                .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.FileUrl)));
            CreateMap<UpdateResolutionDto, Resolution>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Title)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Summary)))
                .ForMember(dest => dest.FileUrl, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.FileUrl)));

            // DotThanhToan mappings
            CreateMap<DotThanhToan, DotThanhToanDto>();
            CreateMap<CreateDotThanhToanDto, DotThanhToan>();

            // HopDong mappings
            CreateMap<HopDong, HopDongDto>()
                .ForMember(dest => dest.GoiThauName, opt => opt.MapFrom(src => src.GoiThau != null ? src.GoiThau.Name : null))
                .ForMember(dest => dest.DuAnName, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Name : null))
                .ForMember(dest => dest.DotThanhToans, opt => opt.MapFrom(src => src.DotThanhToans.OrderBy(d => d.CreatedAt).ToList()));
            CreateMap<CreateHopDongDto, HopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()); // Handled manually in service
            CreateMap<UpdateHopDongDto, HopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()); // Handled manually in service

            // PhongBan mappings
            CreateMap<PhongBan, PhongBanDto>();
            CreateMap<CreatePhongBanDto, PhongBan>();
            CreateMap<UpdatePhongBanDto, PhongBan>();

            // ChucVu mappings
            CreateMap<ChucVu, ChucVuDto>();
            CreateMap<CreateChucVuDto, ChucVu>();
            CreateMap<UpdateChucVuDto, ChucVu>();

            // NhomDuAn mappings
            CreateMap<NhomDuAn, NhomDuAnDto>();
            CreateMap<CreateNhomDuAnDto, NhomDuAn>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdateNhomDuAnDto, NhomDuAn>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));

            // PhanLoaiDuAn mappings
            CreateMap<PhanLoaiDuAn, PhanLoaiDuAnDto>();
            CreateMap<CreatePhanLoaiDuAnDto, PhanLoaiDuAn>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdatePhanLoaiDuAnDto, PhanLoaiDuAn>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));

            // CongViecGoiThau mappings
            CreateMap<CongViecNguoiLienQuan, CongViecNguoiLienQuanDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.SoGioConLai, opt => opt.MapFrom(src => (src.HanXacNhanAt - DateTime.UtcNow).TotalHours > 0 ? Math.Round((src.HanXacNhanAt - DateTime.UtcNow).TotalHours, 1) : 0))
                .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => (src.TrangThaiXacNhan == "Pending" && DateTime.UtcNow > src.HanXacNhanAt) || src.TrangThaiXacNhan == "Overdue"));

            CreateMap<CongViecGoiThau, CongViecGoiThauDto>()
                .ForMember(dest => dest.NguoiLienQuanIds, opt => opt.MapFrom(src => src.NguoiLienQuans != null ? src.NguoiLienQuans.Select(n => n.UserId).ToList() : new List<Guid>()))
                .ForMember(dest => dest.NguoiLienQuans, opt => opt.MapFrom(src => src.NguoiLienQuans));

            CreateMap<CreateCongViecGoiThauDto, CongViecGoiThau>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code ?? string.Empty)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Name) ? src.Name : src.TenTaiLieu))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.GhiChu));
            CreateMap<UpdateCongViecGoiThauDto, CongViecGoiThau>()
                .ForMember(dest => dest.Code, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Name) ? src.Name : src.TenTaiLieu))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.GhiChu));

            // License mappings
            CreateMap<License, LicenseDto>()
                .ForMember(dest => dest.DuAnName, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Name : null))
                .ForMember(dest => dest.DuAnCode, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Code : null))
                .ForMember(dest => dest.HopDongName, opt => opt.MapFrom(src => src.HopDong != null ? src.HopDong.Name : null))
                .ForMember(dest => dest.HopDongCode, opt => opt.MapFrom(src => src.HopDong != null ? src.HopDong.Code : null))
                .ForMember(dest => dest.NhaCungCapName, opt => opt.MapFrom(src => src.NhaCungCap != null ? src.NhaCungCap.Name : null));

            CreateMap<CreateLicenseDto, License>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThongTinThietBi, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThongTinThietBi)))
                .ForMember(dest => dest.GhiChu, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.GhiChu)));

            CreateMap<UpdateLicenseDto, License>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThongTinThietBi, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThongTinThietBi)))
                .ForMember(dest => dest.GhiChu, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.GhiChu)));

            // CommentCongViecGoiThau mappings
            CreateMap<CommentMention, UserMentionDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.MentionedUserId))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.Username : string.Empty))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.FullName : string.Empty));

            CreateMap<User, UserMentionDto>();

            CreateMap<CommentCongViecGoiThau, CommentCongViecGoiThauDto>()
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.CongViecGoiThauId))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Unknown"))
                .ForMember(dest => dest.UserUsername, opt => opt.MapFrom(src => src.User != null ? src.User.Username : "Unknown"))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.IsDeleted ? "Bình luận này đã bị xóa." : src.Content))
                .ForMember(dest => dest.Mentions, opt => opt.MapFrom(src => src.Mentions))
                .ForMember(dest => dest.Replies, opt => opt.Ignore());
        }

    }
}
