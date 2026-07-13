using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class DoiTacService : DbCrudService<DoiTac, DoiTacDto, CreateDoiTacDto, UpdateDoiTacDto>, IDoiTacService
{
    public DoiTacService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }
}
