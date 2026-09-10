using System.Collections.Generic;
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
    Task<byte[]> ExportCongViecGoiThauReportCsvAsync(Guid idGoiThau, string? donViTinh = null);
    Task<byte[]> ExportCongViecGoiThauReportHtmlAsync(Guid idGoiThau, string? donViTinh = null);

    Task<ContractPaymentReportResponseDto> GetContractPaymentReportAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportExcelAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportCsvAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportContractPaymentReportHtmlAsync(int year, int? loaiHopDong, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);

    Task<TheoDoiHopDongReportResponseDto> GetTheoDoiHopDongReportAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportTheoDoiHopDongReportExcelAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportTheoDoiHopDongReportCsvAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);
    Task<byte[]> ExportTheoDoiHopDongReportHtmlAsync(int? year, DateTime? cutoffDate, List<Guid>? loaiHopDongIds, string? search, string? donViTinh = null);

    Task<KeHoachVonReportResponseDto> GetKeHoachVonReportAsync(int? year, int? phuLuc, string? donViTinh = null);
    Task<byte[]> ExportKeHoachVonReportExcelAsync(int? year, int? phuLuc, string? donViTinh = null);
    Task<byte[]> ExportKeHoachVonReportCsvAsync(int? year, int? phuLuc, string? donViTinh = null);
    Task<byte[]> ExportKeHoachVonReportHtmlAsync(int? year, int? phuLuc, string? donViTinh = null);

    Task<KeHoachVonCnttReportResponseDto> GetKeHoachVonCnttReportAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null, string? keyword = null, string? projectType = null);
    Task<byte[]> ExportKeHoachVonCnttReportExcelAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null, string? keyword = null, string? projectType = null);
    Task<byte[]> ExportKeHoachVonCnttReportCsvAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null, string? keyword = null, string? projectType = null);
    Task<byte[]> ExportKeHoachVonCnttReportHtmlAsync(int? fromYear, int? toYear, int? groupStatus, string? donViTinh = null, string? keyword = null, string? projectType = null);

    Task<LicenseSlaReportResponseDto> GetLicenseSlaReportAsync(int? statusFilter = null, string? search = null, string? donViTinh = null);
    Task<byte[]> ExportLicenseSlaReportExcelAsync(int? statusFilter = null, string? search = null, string? donViTinh = null);
    Task<byte[]> ExportLicenseSlaReportCsvAsync(int? statusFilter = null, string? search = null, string? donViTinh = null);
    Task<byte[]> ExportLicenseSlaReportHtmlAsync(int? statusFilter = null, string? search = null, string? donViTinh = null);

    Task<GoiThauLcntReportResponseDto> GetGoiThauLcntReportAsync(int? year = null, Guid? duAnId = null, string? search = null, string? donViTinh = null);
}


