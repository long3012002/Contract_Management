using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ICongViecGoiThauService : ICrudDetailService<CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>
{
    Task<CongViecGoiThauReportDto> GetReportByGoiThauIdAsync(Guid idGoiThau);
    Task<bool> ConfirmCongViecAsync(Guid id, Guid userId);
    Task<(bool Success, string Message)> ForwardStakeholdersAsync(Guid id, List<Guid> userIds, Guid? currentUserId = null, string? ghiChu = null);
    Task<List<CongViecLichSuChuyenTiepDto>> GetForwardHistoryAsync(Guid id);
}
