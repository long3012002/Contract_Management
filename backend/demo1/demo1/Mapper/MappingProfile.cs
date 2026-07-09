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
                    src.DuToanPheDuyet + (src.DieuChinhs != null ? src.DieuChinhs.Sum(dc => dc.GiaTriDieuChinh) : 0)));
            CreateMap<CreateDuAnDto, DuAn>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.TrangThai, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.TrangThai)))
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
                .ForMember(dest => dest.TrangThai, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.TrangThai)))
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

            // Partner mappings
            CreateMap<Partner, PartnerDto>();
            CreateMap<CreatePartnerDto, Partner>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.TaxCode)))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Phone)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Email)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Address)));
            CreateMap<UpdatePartnerDto, Partner>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.TaxCode)))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Phone)))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Email)))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Address)));

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

            // Contract mappings
            CreateMap<Contract, ContractDto>()
                .ForMember(dest => dest.ContractNumber, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name));
            CreateMap<CreateContractDto, Contract>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.ContractNumber)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Title)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Status)));
            CreateMap<UpdateContractDto, Contract>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Title)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Status)));

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
        }
    }
}
