using System;
using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface ICongViecGoiThauService : ICrudDetailService<CongViecGoiThauDto, CreateCongViecGoiThauDto, UpdateCongViecGoiThauDto>
{
    Task<CongViecGoiThauReportDto> GetReportByGoiThauIdAsync(Guid idGoiThau);
    Task<bool> ConfirmCongViecAsync(Guid id, Guid userId);
}
