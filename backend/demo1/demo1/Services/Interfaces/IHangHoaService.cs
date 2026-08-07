using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs.HangHoaDichVu;

namespace demo1.Services.Interfaces;

public interface IHangHoaService : ICrudService<HangHoaDichVuDto, CreateHangHoaDichVuDto, UpdateHangHoaDichVuDto>
{
    Task<IEnumerable<HangHoaDichVuDto>> GetByIdParentAsync(Guid idParent);
}
