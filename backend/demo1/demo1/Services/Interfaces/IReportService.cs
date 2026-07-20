using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IReportService
{
    Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period);
    Task<byte[]> ExportInvestmentReportExcelAsync(int year, int period);
    Task<byte[]> ExportInvestmentReportCsvAsync(int year, int period);
    Task<byte[]> ExportInvestmentReportHtmlAsync(int year, int period);

    Task<CongViecGoiThauReportDto> GetCongViecGoiThauReportAsync(Guid idGoiThau);
    Task<byte[]> ExportCongViecGoiThauReportExcelAsync(Guid idGoiThau);
}

