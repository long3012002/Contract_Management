using System.Threading.Tasks;
using demo1.DTOs;

namespace demo1.Services.Interfaces;

public interface IReportService
{
    Task<ReportResponseDto> GetInvestmentReportAsync(int year, int period, string? donViTinh = null);
    Task<byte[]> ExportInvestmentReportExcelAsync(int year, int period, string? donViTinh = null);
    Task<byte[]> ExportInvestmentReportCsvAsync(int year, int period, string? donViTinh = null);
    Task<byte[]> ExportInvestmentReportHtmlAsync(int year, int period, string? donViTinh = null);

    Task<CongViecGoiThauReportDto> GetCongViecGoiThauReportAsync(Guid idGoiThau, string? donViTinh = null);
    Task<byte[]> ExportCongViecGoiThauReportExcelAsync(Guid idGoiThau, string? donViTinh = null);

    Task<ContractPaymentReportResponseDto> GetContractPaymentReportAsync(int year, int? loaiHopDong, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportExcelAsync(int year, int? loaiHopDong, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportCsvAsync(int year, int? loaiHopDong, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportHtmlAsync(int year, int? loaiHopDong, string? search, string? donViTinh = null);

    Task<TheoDoiHopDongReportResponseDto> GetTheoDoiHopDongReportAsync(int? year, DateTime? cutoffDate, int? loaiHopDong, string? search, string? donViTinh = null);
    Task<byte[]> ExportTheoDoiHopDongReportExcelAsync(int? year, DateTime? cutoffDate, int? loaiHopDong, string? search, string? donViTinh = null);
}


