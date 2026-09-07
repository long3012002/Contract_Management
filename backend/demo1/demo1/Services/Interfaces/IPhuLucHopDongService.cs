using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;
using demo1.Entity;

namespace demo1.Services.Interfaces;

public interface IPhuLucHopDongService
{
    Task<List<PhuLucHopDongDto>> GetByHopDongIdAsync(Guid hopDongId, TrangThaiPhuLuc? trangThai = null);
    Task<PhuLucHopDongDto?> GetByIdAsync(Guid id);
    Task<PhuLucHopDongDto> CreateAsync(CreatePhuLucHopDongDto dto);
    Task<PhuLucHopDongDto?> UpdateAsync(Guid id, UpdatePhuLucHopDongDto dto);
    Task<bool> UpdateStatusAsync(Guid id, TrangThaiPhuLuc trangThai);
    Task<bool> DeleteAsync(Guid id);
}
