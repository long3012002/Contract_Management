using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using demo1.Data;

namespace demo1.Services.Implements;

public abstract class DbCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
    : ICrudService<TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity
    where TDto : class, IHasId
{
    protected readonly AppDbContext DbContext;
    protected readonly IMapper Mapper;
    protected readonly DbSet<TEntity> DbSet;

    protected DbCrudService(AppDbContext dbContext, IMapper mapper)
    {
        DbContext = dbContext;
        Mapper = mapper;
        DbSet = dbContext.Set<TEntity>();
    }

    public virtual async Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<TEntity> query = DbSet;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = Mapper.Map<List<TDto>>(items);

        return new PagedResult<TDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public virtual async Task<IReadOnlyList<TDto>> GetAllItemsAsync()
    {
        var items = await DbSet.ToListAsync();
        return Mapper.Map<List<TDto>>(items);
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        return entity is null ? null : Mapper.Map<TDto>(entity);
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto dto)
    {
        var entity = CreateEntity(dto);
        await EnsureCodeIsUniqueAsync(entity.Code);

        entity.CreatedAt = DateTime.UtcNow;
        await DbSet.AddAsync(entity);
        await DbContext.SaveChangesAsync();

        return Mapper.Map<TDto>(entity);
    }

    public virtual async Task<IEnumerable<TDto>> CreateRangeAsync(IEnumerable<TCreateDto> dtos)
    {
        var entities = new List<TEntity>();
        foreach (var dto in dtos)
        {
            var entity = CreateEntity(dto);
            await EnsureCodeIsUniqueAsync(entity.Code);
            entity.CreatedAt = DateTime.UtcNow;
            entities.Add(entity);
        }
        await DbSet.AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
        return Mapper.Map<List<TDto>>(entities);
    }

    public virtual async Task<bool> UpdateAsync(Guid id, TUpdateDto dto)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        UpdateEntity(entity, dto);
        entity.UpdatedAt = DateTime.UtcNow;

        DbSet.Update(entity);
        await DbContext.SaveChangesAsync();

        return true;
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity is null)
        {
            return false;
        }

        DbSet.Remove(entity);
        await DbContext.SaveChangesAsync();
        return true;
    }

    protected virtual IQueryable<TEntity> ApplySearchFilter(IQueryable<TEntity> query, string keyword)
    {
        return query.Where(item => 
            EF.Functions.Like(item.Code, $"%{keyword}%") || 
            EF.Functions.Like(item.Name, $"%{keyword}%") ||
            (item.Description != null && EF.Functions.Like(item.Description, $"%{keyword}%")));
    }

    protected virtual TDto ToDto(TEntity entity) => Mapper.Map<TDto>(entity);
    protected virtual TEntity CreateEntity(TCreateDto dto) => Mapper.Map<TEntity>(dto);
    protected virtual void UpdateEntity(TEntity entity, TUpdateDto dto) => Mapper.Map(dto, entity);

    private async Task EnsureCodeIsUniqueAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var exists = await DbSet.AnyAsync(item =>
            item.Code.ToLower() == code.ToLower());

        if (exists)
        {
            throw new InvalidOperationException($"Mã '{code}' đã tồn tại.");
        }
    }
}
