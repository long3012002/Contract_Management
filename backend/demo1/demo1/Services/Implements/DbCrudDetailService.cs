using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public abstract class DbCrudDetailService<TEntity, TDto, TCreateDto, TUpdateDto>
    : DbCrudService<TEntity, TDto, TCreateDto, TUpdateDto>, ICrudDetailService<TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity, IHasParentId
    where TDto : class, IHasId, IHasParentId
{
    protected DbCrudDetailService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
    }

    public virtual async Task<IEnumerable<TDto>> GetByParentIdAsync(Guid parentId)
    {
        var entities = await DbSet.AsNoTracking()
            .Where(e => e.ParentId == parentId)
            .ToListAsync();

        return Mapper.Map<List<TDto>>(entities);
    }

    public virtual async Task<PagedResult<TDto>> GetByParentIdPagedAsync(
        Guid parentId, string? search, int page, int pageSize, string? cursor = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<TEntity> query = DbSet.AsNoTracking().Where(e => e.ParentId == parentId);

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

    public virtual async Task<bool> DeleteByParentIdAsync(Guid parentId)
    {
        var entities = await DbSet.Where(e => e.ParentId == parentId).ToListAsync();
        if (!entities.Any())
        {
            return false;
        }

        DbSet.RemoveRange(entities);
        await DbContext.SaveChangesAsync();
        return true;
    }
}
