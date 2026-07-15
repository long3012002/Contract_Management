using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class NhomDuAnService : DbCrudService<NhomDuAn, NhomDuAnDto, CreateNhomDuAnDto, UpdateNhomDuAnDto>, INhomDuAnService
{
    public NhomDuAnService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
