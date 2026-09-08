using AutoMapper;
using demo1.DTOs;
using demo1.Entity;
using demo1.DTOs.HangHoaDichVu;

namespace demo1.Mapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // DuAnPhanKyVon mappings
            CreateMap<DuAnPhanKyVon, DuAnPhanKyVonDto>();
            CreateMap<CreateDuAnPhanKyVonDto, DuAnPhanKyVon>();

            // DuAnNguonVon mappings
            CreateMap<DuAnNguonVon, DuAnNguonVonDto>()
                .ForMember(dest => dest.MaNguonVon, opt => opt.MapFrom(src => src.NguonVon != null ? src.NguonVon.Code : null))
                .ForMember(dest => dest.TenNguonVon, opt => opt.MapFrom(src => src.NguonVon != null ? src.NguonVon.Name : null));
            CreateMap<CreateDuAnNguonVonDto, DuAnNguonVon>();

            // DuAn mappings
            CreateMap<DuAn, DuAnDto>()
                .ForMember(dest => dest.TongDuToanHienTai, opt => opt.MapFrom(src => 
                    src.DuToanPheDuyet + (src.DieuChinhs != null ? src.DieuChinhs.Sum(dc => dc.GiaTriDieuChinh) : 0)))
                .ForMember(dest => dest.NhomDuAnName, opt => opt.MapFrom(src => src.NhomDuAn != null ? src.NhomDuAn.Name : null))
                .ForMember(dest => dest.PhanLoaiDuAnName, opt => opt.MapFrom(src => src.PhanLoaiDuAn != null ? src.PhanLoaiDuAn.Name : null))
                .ForMember(dest => dest.ChuDuAnName, opt => opt.MapFrom(src => src.ChuDuAn != null ? src.ChuDuAn.FullName : null))
                .ForMember(dest => dest.ProjectManager, opt => opt.MapFrom(src => src.ChuDuAn))
                .ForMember(dest => dest.PhanKyVons, opt => opt.MapFrom(src => src.PhanKyVons.OrderBy(p => p.Nam).ToList()))
                .ForMember(dest => dest.DanhSachNguonVon, opt => opt.MapFrom(src => src.DanhSachNguonVon.ToList()));
            CreateMap<DuAn, DuAnNguonSummaryDto>()
                .ForMember(dest => dest.TongDuToanHienTai, opt => opt.MapFrom(src => 
                    src.DuToanPheDuyet + (src.DieuChinhs != null ? src.DieuChinhs.Sum(dc => dc.GiaTriDieuChinh) : 0)));
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
                .ForMember(dest => dest.NguonDuAns, opt => opt.Ignore()) // Will be managed in service
                .ForMember(dest => dest.PhanKyVons, opt => opt.Ignore()); // Will be managed in service
            CreateMap<UpdateDuAnDto, DuAn>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ChuDauTu, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ChuDauTu)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.ThoiGianThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiGianThucHien)))
                .ForMember(dest => dest.NoiDung, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.NoiDung)))
                .ForMember(dest => dest.HinhThucQuanLy, opt => opt.MapFrom(src => src.HinhThucQuanLy))
                .ForMember(dest => dest.ToChucThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ToChucThucHien)))
                .ForMember(dest => dest.PhanKyVons, opt => opt.Ignore()); // Will be managed in service

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

            CreateMap<NhaThauGoiThau, NhaThauGoiThauDto>()
                .ForMember(dest => dest.NhaThauName, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Name : null))
                .ForMember(dest => dest.NhaThauCode, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Code : null))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.TaxCode : null))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Phone : null))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Email : null))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Address : null))
                .ForMember(dest => dest.Account, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Account : null))
                .ForMember(dest => dest.Representative, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Representative : null))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Position : null));
            CreateMap<NhaThauGoiThauInputDto, NhaThauGoiThau>()
                    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
                    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.HopDong, opt => opt.Ignore())
                .ForMember(dest => dest.NhaThau, opt => opt.Ignore());

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

            // LoaiHopDong mappings
            CreateMap<LoaiHopDong, LoaiHopDongDto>();
            CreateMap<CreateLoaiHopDongDto, LoaiHopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdateLoaiHopDongDto, LoaiHopDong>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));

            // HopDong mappings
            CreateMap<HopDong, HopDongDto>()
                .ForMember(dest => dest.GoiThauName, opt => opt.MapFrom(src => src.GoiThau != null ? src.GoiThau.Name : null))
                .ForMember(dest => dest.DuAnName, opt => opt.MapFrom(src => src.DuAn != null ? src.DuAn.Name : null))
                .ForMember(dest => dest.LoaiHopDongName, opt => opt.MapFrom(src => src.LoaiHopDongNavigation != null ? src.LoaiHopDongNavigation.Name : null))
                .ForMember(dest => dest.NhaThauName, opt => opt.MapFrom(src => src.NhaThau != null ? src.NhaThau.Name : null))
                .ForMember(dest => dest.DotThanhToans, opt => opt.MapFrom(src => src.DotThanhToans.OrderBy(d => d.CreatedAt).ToList()))
                .ForMember(dest => dest.NhaThauGoiThaus, opt => opt.MapFrom(src => src.NhaThauGoiThaus))
                .ForMember(dest => dest.HangHoaDichVus, opt => opt.MapFrom(src => src.HangHoaDichVus))
                .ForMember(dest => dest.PhuLucHopDongs, opt => opt.MapFrom(src => src.PhuLucHopDongs.OrderByDescending(p => p.NgayKy).ToList()));
            CreateMap<PhuLucHopDong, PhuLucHopDongDto>();
            CreateMap<CreateHopDongDto, HopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()) // Handled manually in service
                .ForMember(dest => dest.NhaThauGoiThaus, opt => opt.Ignore()); // Handled manually in service
            CreateMap<UpdateHopDongDto, HopDong>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)))
                .ForMember(dest => dest.ThoiHanThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.ThoiHanThucHien)))
                .ForMember(dest => dest.DiaDiemThucHien, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.DiaDiemThucHien)))
                .ForMember(dest => dest.DotThanhToans, opt => opt.Ignore()) // Handled manually in service
                .ForMember(dest => dest.NhaThauGoiThaus, opt => opt.Ignore()); // Handled manually in service

            // PhongBan mappings
            CreateMap<PhongBan, PhongBanDto>();
            CreateMap<CreatePhongBanDto, PhongBan>();
            CreateMap<UpdatePhongBanDto, PhongBan>();

            // ToNhom mappings
            CreateMap<ToNhom, ToNhomDto>()
                .ForMember(dest => dest.TenPhongBan, opt => opt.MapFrom(src => src.PhongBan != null ? src.PhongBan.TenPhongBan : null));
            CreateMap<CreateToNhomDto, ToNhom>();
            CreateMap<UpdateToNhomDto, ToNhom>();

            // ChucVu mappings
            CreateMap<ChucVu, ChucVuDto>();
            CreateMap<CreateChucVuDto, ChucVu>();
            CreateMap<UpdateChucVuDto, ChucVu>();

            // DonVi mappings
            CreateMap<DonVi, DonViDto>();
            CreateMap<CreateDonViDto, DonVi>();
            CreateMap<UpdateDonViDto, DonVi>();

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

            // NguonVon mappings
            CreateMap<NguonVon, NguonVonDto>();
            CreateMap<CreateNguonVonDto, NguonVon>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));
            CreateMap<UpdateNguonVonDto, NguonVon>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => MapperHelpers.TrimRequired(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => MapperHelpers.TrimOptional(src.Description)));

            // XuatXu mappings
            CreateMap<XuatXu, XuatXuDto>();
            CreateMap<CreateXuatXuDto, XuatXu>();
            CreateMap<UpdateXuatXuDto, XuatXu>();

            // DonViTinh mappings
            CreateMap<DonViTinh, DonViTinhDto>();
            CreateMap<CreateDonViTinhDto, DonViTinh>();
            CreateMap<UpdateDonViTinhDto, DonViTinh>();

            // HangSanXuat mappings
            CreateMap<HangSanXuat, HangSanXuatDto>();
            CreateMap<CreateHangSanXuatDto, HangSanXuat>();
            CreateMap<UpdateHangSanXuatDto, HangSanXuat>();

            // HangHoaDichVu mappings
            CreateMap<HangHoaDichVu, HangHoaDichVuDto>()
                .ForMember(dest => dest.TenXuatXu, opt => opt.MapFrom(src => src.XuatXu != null ? src.XuatXu.Name : null))
                .ForMember(dest => dest.TenHangSanXuat, opt => opt.MapFrom(src => src.HangSanXuat != null ? src.HangSanXuat.Name : null))
                .ForMember(dest => dest.TenLicense, opt => opt.MapFrom(src => src.License != null ? src.License.Name : null))
                .ForMember(dest => dest.TenDonViTinh, opt => opt.MapFrom(src => src.DonViTinh != null ? src.DonViTinh.Name : null));
            CreateMap<CreateHangHoaDichVuDto, HangHoaDichVu>();
            CreateMap<UpdateHangHoaDichVuDto, HangHoaDichVu>();

            // CongViecGoiThau mappings
            CreateMap<CongViecNguoiLienQuan, CongViecNguoiLienQuanDto>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : null))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : null))
                .ForMember(dest => dest.TenPhongBan, opt => opt.MapFrom(src => src.User != null ? src.User.TenPhongBan : null))
                .ForMember(dest => dest.TenDonVi, opt => opt.MapFrom(src => src.User != null ? src.User.TenDonVi : null))
                .ForMember(dest => dest.TenChucVu, opt => opt.MapFrom(src => src.User != null ? src.User.TenChucVu : null))
                .ForMember(dest => dest.SoGioConLai, opt => opt.MapFrom(src => (src.HanXacNhanAt - DateTime.UtcNow).TotalHours > 0 ? Math.Round((src.HanXacNhanAt - DateTime.UtcNow).TotalHours, 1) : 0))
                .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => (src.TrangThaiXacNhan == "Pending" && DateTime.UtcNow > src.HanXacNhanAt) || src.TrangThaiXacNhan == "Overdue"));

            CreateMap<CongViecLichSuChuyenTiep, CongViecLichSuChuyenTiepDto>()
                .ForMember(dest => dest.FromUserFullName, opt => opt.MapFrom(src => src.FromUser != null ? src.FromUser.FullName : null))
                .ForMember(dest => dest.FromUsername, opt => opt.MapFrom(src => src.FromUser != null ? src.FromUser.Username : null))
                .ForMember(dest => dest.ToUserFullName, opt => opt.MapFrom(src => src.ToUser != null ? src.ToUser.FullName : null))
                .ForMember(dest => dest.ToUsername, opt => opt.MapFrom(src => src.ToUser != null ? src.ToUser.Username : null));

            CreateMap<CongViecGoiThau, CongViecGoiThauDto>()
                .ForMember(dest => dest.NguoiLienQuanIds, opt => opt.MapFrom(src => src.NguoiLienQuans != null ? src.NguoiLienQuans.Select(n => n.UserId).ToList() : new List<Guid>()))
                .ForMember(dest => dest.NguoiLienQuans, opt => opt.MapFrom(src => src.NguoiLienQuans))
                .ForMember(dest => dest.LichSuChuyenTieps, opt => opt.MapFrom(src => src.LichSuChuyenTieps))
                .ForMember(dest => dest.CreateUserFullName, opt => opt.MapFrom(src => src.CreateUser != null ? src.CreateUser.FullName : null))
                .ForMember(dest => dest.ModifiedUserFullName, opt => opt.MapFrom(src => src.ModifiedUser != null ? src.ModifiedUser.FullName : null));

            CreateMap<CreateCongViecGoiThauDto, CongViecGoiThau>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => MapperHelpers.NormalizeCode(src.Code ?? string.Empty)))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Name) ? src.Name : src.TenTaiLieu))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.GhiChu));
            CreateMap<UpdateCongViecGoiThauDto, CongViecGoiThau>()
                .ForMember(dest => dest.Code, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Code)))
                .ForMember(dest => dest.TenTaiLieu, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.TenTaiLieu)))
                .ForMember(dest => dest.Name, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Name)))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.GhiChu))
                .ForMember(dest => dest.GoiThauId, opt => opt.Condition(src => src.GoiThauId.HasValue && src.GoiThauId.Value != Guid.Empty))
                .ForMember(dest => dest.IsActive, opt => opt.Condition(src => src.IsActive.HasValue))
                // NguoiLienQuans is handled manually in UpdateAsync — ignore here to prevent AutoMapper
                // from clearing/corrupting the navigation collection before manual Add/Remove logic runs.
                .ForMember(dest => dest.NguoiLienQuans, opt => opt.Ignore());

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
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.FullName : string.Empty))
                .ForMember(dest => dest.TenPhongBan, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.TenPhongBan : null))
                .ForMember(dest => dest.TenDonVi, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.TenDonVi : null))
                .ForMember(dest => dest.TenChucVu, opt => opt.MapFrom(src => src.MentionedUser != null ? src.MentionedUser.TenChucVu : null));

            CreateMap<User, UserMentionDto>();
            CreateMap<User, UserSummaryDto>();

            CreateMap<CommentCongViecGoiThau, CommentCongViecGoiThauDto>()
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.CongViecGoiThauId))
                .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Unknown"))
                .ForMember(dest => dest.UserUsername, opt => opt.MapFrom(src => src.User != null ? src.User.Username : "Unknown"))
                .ForMember(dest => dest.UserTenPhongBan, opt => opt.MapFrom(src => src.User != null ? src.User.TenPhongBan : null))
                .ForMember(dest => dest.UserTenDonVi, opt => opt.MapFrom(src => src.User != null ? src.User.TenDonVi : null))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.IsDeleted ? "Bình luận này đã bị xóa." : src.Content))
                .ForMember(dest => dest.Mentions, opt => opt.MapFrom(src => src.Mentions))
                .ForMember(dest => dest.Replies, opt => opt.Ignore());
        }

    }
}
