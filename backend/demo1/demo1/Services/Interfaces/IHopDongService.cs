using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IHopDongService : ICrudService<HopDongDto, CreateHopDongDto, UpdateHopDongDto>
{
    Task<PagedResult<HopDongDto>> GetAllAsync(HopDongFilterDto filter);
    Task<bool> ConfirmPaymentAsync(Guid dotThanhToanId);
}

