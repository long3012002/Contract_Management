using demo1.DTOs.DanhMuc;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces.DanhMuc;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements.DanhMuc;

public class HangSanXuatService : DbCrudService<HangSanXuat, HangSanXuatDto, CreateHangSanXuatDto, UpdateHangSanXuatDto>, IHangSanXuatService
{
    public HangSanXuatService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
