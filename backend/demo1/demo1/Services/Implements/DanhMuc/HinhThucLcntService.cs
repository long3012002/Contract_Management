using demo1.DTOs;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class HinhThucLcntService : DbCrudService<HinhThucLcnt, HinhThucLcntDto, CreateHinhThucLcntDto, UpdateHinhThucLcntDto>, IHinhThucLcntService
{
    public HinhThucLcntService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
