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
    public class DonViService : IDonViService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;

        public DonViService(AppDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DonViDto>> GetAllAsync()
        {
            var items = await _dbContext.DonVis.OrderBy(dv => dv.TenDonVi).ToListAsync();
            return _mapper.Map<IEnumerable<DonViDto>>(items);
        }

        public async Task<DonViDto?> GetByIdAsync(Guid id)
        {
            var item = await _dbContext.DonVis.FindAsync(id);
            return item == null ? null : _mapper.Map<DonViDto>(item);
        }

        public async Task<DonViDto> CreateAsync(CreateDonViDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDonVi))
            {
                throw new ArgumentException("Tên đơn vị là bắt buộc.");
            }

            var exists = await _dbContext.DonVis.AnyAsync(dv => dv.TenDonVi.ToLower() == dto.TenDonVi.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên đơn vị đã tồn tại.");
            }

            var item = new DonVi
            {
                TenDonVi = dto.TenDonVi.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.DonVis.Add(item);
            await _dbContext.SaveChangesAsync();
            return _mapper.Map<DonViDto>(item);
        }

        public async Task<IEnumerable<DonViDto>> CreateRangeAsync(IEnumerable<CreateDonViDto> dtos)
        {
            var result = new List<DonViDto>();
            foreach (var dto in dtos)
            {
                var created = await CreateAsync(dto);
                result.Add(created);
            }
            return result;
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateDonViDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.TenDonVi))
            {
                throw new ArgumentException("Tên đơn vị là bắt buộc.");
            }

            var item = await _dbContext.DonVis.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            var exists = await _dbContext.DonVis.AnyAsync(dv => dv.Id != id && dv.TenDonVi.ToLower() == dto.TenDonVi.Trim().ToLower());
            if (exists)
            {
                throw new InvalidOperationException("Tên đơn vị đã tồn tại.");
            }

            item.TenDonVi = dto.TenDonVi.Trim();
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var item = await _dbContext.DonVis.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            var users = await _dbContext.Users.Where(u => u.IdDonVi == id).ToListAsync();
            foreach (var user in users)
            {
                user.IdDonVi = null;
                user.TenDonVi = null;
            }

            _dbContext.DonVis.Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
