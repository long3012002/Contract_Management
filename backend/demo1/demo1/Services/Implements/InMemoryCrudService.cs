using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements;

public abstract class InMemoryCrudService<TEntity, TDto, TCreateDto, TUpdateDto>
    : ICrudService<TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity
    where TDto : IHasId
{
    private readonly List<TEntity> _items = new();
    private readonly object _syncRoot = new();
    private int _nextId = 1;

    public Task<PagedResult<TDto>> GetAllAsync(string? search, int page, int pageSize)
    {
        lock (_syncRoot)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(item =>
                    GetSearchText(item).Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var totalItems = query.Count();
            var items = query
                .OrderByDescending(item => item.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToList();

            return Task.FromResult(new PagedResult<TDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems
            });
        }
    }

    public Task<IReadOnlyList<TDto>> GetAllItemsAsync()
    {
        lock (_syncRoot)
        {
            return Task.FromResult<IReadOnlyList<TDto>>(_items.Select(ToDto).ToList());
        }
    }

    public Task<TDto?> GetByIdAsync(int id)
    {
        lock (_syncRoot)
        {
            var entity = _items.FirstOrDefault(item => item.Id == id);
            return Task.FromResult(entity is null ? default : ToDto(entity));
        }
    }

    public Task<TDto> CreateAsync(TCreateDto dto)
    {
        lock (_syncRoot)
        {
            var entity = CreateEntity(dto);
            EnsureCodeIsUnique(entity.Code);

            entity.Id = _nextId++;
            entity.CreatedAt = DateTime.UtcNow;
            _items.Add(entity);

            return Task.FromResult(ToDto(entity));
        }
    }

    public Task<bool> UpdateAsync(int id, TUpdateDto dto)
    {
        lock (_syncRoot)
        {
            var entity = _items.FirstOrDefault(item => item.Id == id);
            if (entity is null)
            {
                return Task.FromResult(false);
            }

            UpdateEntity(entity, dto);
            entity.UpdatedAt = DateTime.UtcNow;

            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (_syncRoot)
        {
            var entity = _items.FirstOrDefault(item => item.Id == id);
            if (entity is null)
            {
                return Task.FromResult(false);
            }

            _items.Remove(entity);
            return Task.FromResult(true);
        }
    }

    protected void Seed(params TEntity[] entities)
    {
        lock (_syncRoot)
        {
            foreach (var entity in entities)
            {
                entity.Id = _nextId++;
                entity.CreatedAt = DateTime.UtcNow;
                _items.Add(entity);
            }
        }
    }

    protected virtual string GetSearchText(TEntity entity)
    {
        return $"{entity.Code} {entity.Name} {entity.Description}";
    }

    protected abstract TDto ToDto(TEntity entity);
    protected abstract TEntity CreateEntity(TCreateDto dto);
    protected abstract void UpdateEntity(TEntity entity, TUpdateDto dto);

    private void EnsureCodeIsUnique(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        var exists = _items.Any(item =>
            item.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            throw new InvalidOperationException($"Ma '{code}' da ton tai.");
        }
    }
}
