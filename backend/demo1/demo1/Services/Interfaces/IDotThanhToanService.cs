using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IDotThanhToanService
{
    Task<PagedResult<DotThanhToanReportDto>> GetFilteredPaymentPhasesAsync(DotThanhToanFilterDto filter);
}
