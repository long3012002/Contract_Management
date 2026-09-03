using demo1.DTOs;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class NguonVonService : DbCrudService<NguonVon, NguonVonDto, CreateNguonVonDto, UpdateNguonVonDto>, INguonVonService
{
    public NguonVonService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
