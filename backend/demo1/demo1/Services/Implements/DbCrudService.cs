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

    public virtual async Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<TEntity> query = DbSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = ApplySearchFilter(query, keyword);
        }

        var totalItems = await query.CountAsync();

        List<TEntity> items;
        bool isKeyset = TryParseCursor(cursor, out var lastCreatedAt, out var lastId);

        if (isKeyset)
        {
            items = await query
                .Where(item => item.CreatedAt < lastCreatedAt || (item.CreatedAt == lastCreatedAt && item.Id.CompareTo(lastId) < 0))
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Take(pageSize)
                .ToListAsync();
        }
        else
        {
            items = await query
                .OrderByDescending(item => item.CreatedAt)
                .ThenByDescending(item => item.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        string? nextCursor = null;
        if (items.Any())
        {
            var lastItem = items.Last();
            var hasMore = await query
                .Where(item => item.CreatedAt < lastItem.CreatedAt || (item.CreatedAt == lastItem.CreatedAt && item.Id.CompareTo(lastItem.Id) < 0))
                .AnyAsync();
            if (hasMore)
            {
                nextCursor = EncodeCursor(lastItem.CreatedAt, lastItem.Id);
            }
        }

        var dtos = Mapper.Map<List<TDto>>(items);

        return new PagedResult<TDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            NextCursor = nextCursor
        };
    }

    protected string EncodeCursor(DateTime createdAt, Guid id)
    {
        var plainText = $"{createdAt.ToString("o")}|{id}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(bytes);
    }

    protected bool TryParseCursor(string? cursor, out DateTime createdAt, out Guid id)
    {
        createdAt = DateTime.MinValue;
        id = Guid.Empty;
        if (string.IsNullOrWhiteSpace(cursor)) return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var plainText = System.Text.Encoding.UTF8.GetString(bytes);
            var parts = plainText.Split('|');
            if (parts.Length == 2 && DateTime.TryParse(parts[0], out createdAt) && Guid.TryParse(parts[1], out id))
            {
                return true;
            }
        }
        catch
        {
            // Fail silently
        }
        return false;
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
