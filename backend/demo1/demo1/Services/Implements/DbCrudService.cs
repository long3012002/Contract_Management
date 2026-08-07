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

    protected virtual IQueryable<TEntity> GetQueryable() => DbSet;

    public virtual async Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize, string? cursor = null)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<TEntity> query = GetQueryable().AsNoTracking();

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
        catch (Exception)
        {
            throw;
        }
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
        try
        {
            var items = await GetQueryable().ToListAsync();
            return Mapper.Map<List<TDto>>(items);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await GetQueryable().FirstOrDefaultAsync(e => e.Id == id);
            return entity is null ? null : Mapper.Map<TDto>(entity);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<TDto> CreateAsync(TCreateDto dto)
    {
        try
        {
            var entity = CreateEntity(dto);
            await EnsureCodeIsUniqueAsync(entity.Code);

            entity.CreatedAt = DateTime.UtcNow;
            await DbSet.AddAsync(entity);
            await DbContext.SaveChangesAsync();

            return Mapper.Map<TDto>(entity);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<IEnumerable<TDto>> CreateRangeAsync(IEnumerable<TCreateDto> dtos)
    {
        try
        {
            var dtoList = dtos.ToList();
            var entities = new List<TEntity>();
            var codes = new List<string>();

            foreach (var dto in dtoList)
            {
                var entity = CreateEntity(dto);
                entity.CreatedAt = DateTime.UtcNow;
                entities.Add(entity);

                if (!string.IsNullOrWhiteSpace(entity.Code))
                {
                    codes.Add(entity.Code.Trim().ToLower());
                }
            }

            // Check duplicate codes within the batch itself
            var duplicateCodesInBatch = codes.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateCodesInBatch.Any())
            {
                throw new InvalidOperationException($"Mã bị trùng lặp trong danh sách thêm mới: '{duplicateCodesInBatch.First()}'");
            }

            // Check duplicate codes in the database in a single query
            if (codes.Any())
            {
                var existingCodes = await GetQueryable()
                    .Where(item => codes.Contains(item.Code.ToLower()))
                    .Select(item => item.Code)
                    .ToListAsync();

                if (existingCodes.Any())
                {
                    throw new InvalidOperationException($"Mã '{existingCodes.First()}' đã tồn tại.");
                }
            }

            await DbSet.AddRangeAsync(entities);
            await DbContext.SaveChangesAsync();
            return Mapper.Map<List<TDto>>(entities);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<bool> UpdateAsync(Guid id, TUpdateDto dto)
    {
        try
        {
            var entity = await GetQueryable().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null)
            {
                return false;
            }

            UpdateEntity(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await GetQueryable().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null)
            {
                return false;
            }

            DbSet.Remove(entity);
            await DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<bool> SoftDeleteAsync(Guid id)
    {
        try
        {
            var entity = await GetQueryable().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null || entity.IsDeleted)
            {
                return false;
            }

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            var userId = DbContext.CurrentUserService?.GetUserId();
            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                entity.DeletedByUserId = userId.Value;
            }
            else
            {
                var username = DbContext.CurrentUserService?.GetUsername();
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
                    if (user != null)
                    {
                        entity.DeletedByUserId = user.Id;
                    }
                }
            }

            await DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public virtual async Task<bool> RestoreAsync(Guid id)
    {
        try
        {
            var entity = await GetQueryable().IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null || !entity.IsDeleted)
            {
                return false;
            }

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;
            entity.UpdatedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            throw;
        }
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

        var exists = await GetQueryable().AnyAsync(item =>
            item.Code.ToLower() == code.ToLower());

        if (exists)
        {
            throw new InvalidOperationException($"Mã '{code}' đã tồn tại.");
        }
    }
}
