using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.HangHoaDichVu;
using demo1.Entity;

namespace demo1.Services.Interfaces;

public interface IHangHoaDichVuService : ICrudService<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    Task<IEnumerable<HangHoaDichVuDto>> GetByIdParentAsync(Guid idParent, LoaiHangHoaDichVu? loai = null);
}
