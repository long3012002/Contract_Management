using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IReportService
{
    Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period);
}
