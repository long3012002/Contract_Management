using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity.DanhMuc;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class ToNhomService : IToNhomService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private const string CacheKeyAll = "ToNhom_All";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ToNhomService(AppDbContext dbContext, IMapper mapper, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _cache = cache;
        }

        private void InvalidateCache(Guid? id = null)
        {
            _cache.Remove(CacheKeyAll);
            if (id.HasValue)
            {
                _cache.Remove($"ToNhom_{id.Value}");
            }
        }

        public async Task<IEnumerable<ToNhomDto>> GetAllAsync()
        {
            var cachedItems = await _cache.GetOrCreateAsync(CacheKeyAll, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var items = await _dbContext.ToNhoms
                    .Include(t => t.PhongBan)
                    .OrderBy(t => t.TenToNhom)
                    .ToListAsync();
                return _mapper.Map<IEnumerable<ToNhomDto>>(items);
            });

            return cachedItems ?? Enumerable.Empty<ToNhomDto>();
        }

        public async Task<ToNhomDto?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"ToNhom_{id}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var item = await _dbContext.ToNhoms
                    .Include(t => t.PhongBan)
                    .FirstOrDefaultAsync(t => t.Id == id);
                return item == null ? null : _mapper.Map<ToNhomDto>(item);
            });
        }

        public async Task<ToNhomDto> CreateAsync(CreateToNhomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenToNhom))
            {
                throw new ArgumentException("Tên tổ nhóm là bắt buộc.");
            }

            var exists = await _dbContext.ToNhoms.AnyAsync(t => t.TenToNhom.ToLower() == dto.TenToNhom.Trim().ToLower() && t.IdPhongBan == dto.IdPhongBan);
            if (exists)
            {
                throw new InvalidOperationException("Tên tổ nhóm đã tồn tại trong phòng ban này.");
            }

            var item = new ToNhom
            {
                TenToNhom = dto.TenToNhom.Trim(),
                IdPhongBan = dto.IdPhongBan,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ToNhoms.Add(item);
            await _dbContext.SaveChangesAsync();

            InvalidateCache();
            
            var result = await _dbContext.ToNhoms
                .Include(t => t.PhongBan)
                .FirstOrDefaultAsync(t => t.Id == item.Id);
                
            return _mapper.Map<ToNhomDto>(result!);
        }

        public async Task<IEnumerable<ToNhomDto>> CreateRangeAsync(IEnumerable<CreateToNhomDto> dtos)
        {
            var result = new List<ToNhomDto>();
            foreach (var dto in dtos)
            {
                var created = await CreateAsync(dto);
                result.Add(created);
            }
            InvalidateCache();
            return result;
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateToNhomDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenToNhom))
            {
                throw new ArgumentException("Tên tổ nhóm là bắt buộc.");
            }

            var item = await _dbContext.ToNhoms.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            var exists = await _dbContext.ToNhoms.AnyAsync(t => t.Id != id && t.TenToNhom.ToLower() == dto.TenToNhom.Trim().ToLower() && t.IdPhongBan == dto.IdPhongBan);
            if (exists)
            {
                throw new InvalidOperationException("Tên tổ nhóm đã tồn tại trong phòng ban này.");
            }

            item.TenToNhom = dto.TenToNhom.Trim();
            item.IdPhongBan = dto.IdPhongBan;
            await _dbContext.SaveChangesAsync();

            InvalidateCache(id);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.ToNhoms.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            // Clear user references pointing to this ToNhom as their IdPhongBan
            var users = await _dbContext.Users.Where(u => u.IdPhongBan == id).ToListAsync();
            foreach (var user in users)
            {
                user.IdPhongBan = null;
                user.TenPhongBan = null;
            }

            _dbContext.ToNhoms.Remove(item);
            await _dbContext.SaveChangesAsync();

            InvalidateCache(id);
            return true;
        }
    }
}
