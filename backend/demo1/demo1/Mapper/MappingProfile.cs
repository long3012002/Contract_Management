using AutoMapper;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Project mappings
            CreateMap<Project, ProjectDto>();
            CreateMap<CreateProjectDto, Project>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Status)));
            CreateMap<UpdateProjectDto, Project>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Status)));

            // BidPackage mappings
            CreateMap<BidPackage, BidPackageDto>();
            CreateMap<CreateBidPackageDto, BidPackage>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdateBidPackageDto, BidPackage>()
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
        }
    }
}
