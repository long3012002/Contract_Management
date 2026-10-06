using demo1.DTOs;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class PhuongThucLcntService : DbCrudService<PhuongThucLcnt, PhuongThucLcntDto, CreatePhuongThucLcntDto, UpdatePhuongThucLcntDto>, IPhuongThucLcntService
{
    public PhuongThucLcntService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
