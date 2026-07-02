using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data.Repositories;
using demo1.Entities;

namespace demo1.Services
{
    public class BaseService<TEntity, TResponseDto, TCreateDto, TUpdateDto> 
        : IBaseService<TEntity, TResponseDto, TCreateDto, TUpdateDto> 
        where TEntity : BaseEntity
    {
        protected readonly IRepository<TEntity> _repository;
        protected readonly IMapper _mapper;

        public BaseService(IRepository<TEntity> repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public virtual async Task<IEnumerable<TResponseDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<TResponseDto>>(entities);
        }

        public virtual async Task<TResponseDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return default;
            return _mapper.Map<TResponseDto>(entity);
        }

        public virtual async Task<TResponseDto> CreateAsync(TCreateDto dto)
        {
            var entity = _mapper.Map<TEntity>(dto);
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            return _mapper.Map<TResponseDto>(entity);
        }

        public virtual async Task UpdateAsync(Guid id, TUpdateDto dto)
        {
            var existingEntity = await _repository.GetByIdAsync(id);
            if (existingEntity != null)
            {
                _mapper.Map(dto, existingEntity);
                await _repository.UpdateAsync(existingEntity);
                await _repository.SaveChangesAsync();
            }
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
            await _repository.SaveChangesAsync();
        }
    }
}
