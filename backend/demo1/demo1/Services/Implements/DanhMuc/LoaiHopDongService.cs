using demo1.DTOs;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class LoaiHopDongService : DbCrudService<LoaiHopDong, LoaiHopDongDto, CreateLoaiHopDongDto, UpdateLoaiHopDongDto>, ILoaiHopDongService
{
    public LoaiHopDongService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
