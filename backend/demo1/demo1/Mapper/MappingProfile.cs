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
                .ForMember(dest => dest.DuAnName, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Name : null));
            CreateMap<CreateHopDongDto, HopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()); // Handled manually in service
            CreateMap<UpdateHopDongDto, HopDong>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()); // Handled manually in service

            // PhongBan mappings
            CreateMap<PhongBan, PhongBanDto>();
            CreateMap<CreatePhongBanDto, PhongBan>();
            CreateMap<UpdatePhongBanDto, PhongBan>();
            CreateMap<PhongBanPermission, PhongBanPermissionDto>();
            CreateMap<UpdatePhongBanPermissionDto, PhongBanPermission>();

            // ChucVu mappings
            CreateMap<ChucVu, ChucVuDto>();
            CreateMap<CreateChucVuDto, ChucVu>();
            CreateMap<UpdateChucVuDto, ChucVu>();
            CreateMap<ChucVuPermission, ChucVuPermissionDto>();
            CreateMap<UpdateChucVuPermissionDto, ChucVuPermission>();

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
        }
    }
}
