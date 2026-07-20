using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public ChucVuService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ChucVuDto>> GetAllAsync()
        {
            var items = await _dbContext.ChucVus.OrderBy(cv => cv.TenChucVu).ToListAsync();
            return _mapper.Map<IEnumerable<ChucVuDto>>(items);
        }

        public async Task<ChucVuDto?> GetByIdAsync(Guid id)
        {
            var item = await _dbContext.ChucVus.FindAsync(id);
            return item == null ? null : _mapper.Map<ChucVuDto>(item);
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

            var item = new ChucVu
            {
                TenChucVu = dto.TenChucVu.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ChucVus.Add(item);
            await _dbContext.SaveChangesAsync();
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

            var exists = await _dbContext.ChucVus.AnyAsync(cv => cv.Id != id && cv.TenChucVu.ToLower() == dto.TenChucVu.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên chức vụ đã tồn tại.");
            }

            item.TenChucVu = dto.TenChucVu.Trim();
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.ChucVus.FindAsync(id);
            if (item == null)
            {
                return false;
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
            return true;
        }
    }
}
