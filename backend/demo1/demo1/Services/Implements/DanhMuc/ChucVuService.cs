using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;

namespace demo1.Services.Implements
{
    public class ChucVuService : IChucVuService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private const string CacheKeyAll = "ChucVu_All";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public ChucVuService(AppDbContext dbContext, IMapper mapper, IMemoryCache cache)
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
                _cache.Remove($"ChucVu_{id.Value}");
            }
        }

        public async Task<IEnumerable<ChucVuDto>> GetAllAsync()
        {
            var cachedItems = await _cache.GetOrCreateAsync(CacheKeyAll, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var items = await _dbContext.ChucVus.OrderBy(cv => cv.TenChucVu).ToListAsync();
                return _mapper.Map<IEnumerable<ChucVuDto>>(items);
            });

            return cachedItems ?? Enumerable.Empty<ChucVuDto>();
        }

        public async Task<ChucVuDto?> GetByIdAsync(Guid id)
        {
            string cacheKey = $"ChucVu_{id}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var item = await _dbContext.ChucVus.FindAsync(id);
                return item == null ? null : _mapper.Map<ChucVuDto>(item);
            });
        }

        public async Task<ChucVuDto> CreateAsync(CreateChucVuDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenChucVu))
            {
                throw new ArgumentException("Tên chức vụ là bắt buộc.");
            }

            var exists = await _dbContext.ChucVus.AnyAsync(cv => cv.TenChucVu.ToLower() == dto.TenChucVu.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên chức vụ đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var existsCode = await _dbContext.ChucVus.AnyAsync(cv => cv.Code != null && cv.Code.ToLower() == dto.Code.Trim().ToLower());
                if (existsCode)
                {
                    throw new InvalidOperationException("Mã chức vụ đã tồn tại.");
                }
            }

            var item = new ChucVu
            {
                TenChucVu = dto.TenChucVu.Trim(),
                Code = dto.Code?.Trim() ?? string.Empty,
                Level = dto.Level,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ChucVus.Add(item);
            await _dbContext.SaveChangesAsync();

            InvalidateCache();
            return _mapper.Map<ChucVuDto>(item);
        }

        public async Task<IEnumerable<ChucVuDto>> CreateRangeAsync(IEnumerable<CreateChucVuDto> dtos)
        {
            var result = new List<ChucVuDto>();
            foreach (var dto in dtos)
            {
                var created = await CreateAsync(dto);
                result.Add(created);
            }
            InvalidateCache();
            return result;
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateChucVuDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenChucVu))
            {
                throw new ArgumentException("Tên chức vụ là bắt buộc.");
            }

            var item = await _dbContext.ChucVus.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            // Prevent editing of default positions
            var defaultCodes = new[] { "TGD", "GD", "PGD", "TP", "PP", "CV" };
            if (!string.IsNullOrEmpty(item.Code) && defaultCodes.Contains(item.Code.ToUpper()))
            {
                throw new InvalidOperationException("Không thể chỉnh sửa các chức vụ mặc định của hệ thống.");
            }

            var exists = await _dbContext.ChucVus.AnyAsync(cv => cv.Id != id && cv.TenChucVu.ToLower() == dto.TenChucVu.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên chức vụ đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                var existsCode = await _dbContext.ChucVus.AnyAsync(cv => cv.Id != id && cv.Code != null && cv.Code.ToLower() == dto.Code.Trim().ToLower());
                if (existsCode)
                {
                    throw new InvalidOperationException("Mã chức vụ đã tồn tại.");
                }
            }

            item.TenChucVu = dto.TenChucVu.Trim();
            item.Code = dto.Code?.Trim() ?? string.Empty;
            item.Level = dto.Level;
            await _dbContext.SaveChangesAsync();

            InvalidateCache(id);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.ChucVus.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            // Prevent deletion of default positions
            var defaultCodes = new[] { "TGD", "GD", "PGD", "TP", "PP", "CV" };
            if (!string.IsNullOrEmpty(item.Code) && defaultCodes.Contains(item.Code.ToUpper()))
            {
                throw new InvalidOperationException("Không thể xóa các chức vụ mặc định của hệ thống.");
            }

            // Set User reference to null
            var users = await _dbContext.Users.Where(u => u.IdChucVu == id).ToListAsync();
            foreach (var user in users)
            {
                user.IdChucVu = null;
                user.TenChucVu = null;
            }

            _dbContext.ChucVus.Remove(item);
            await _dbContext.SaveChangesAsync();

            InvalidateCache(id);
            return true;
        }
    }
}
