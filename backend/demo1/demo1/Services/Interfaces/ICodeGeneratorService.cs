using System;
using System.Threading.Tasks;

namespace demo1.Services.Interfaces;

public interface ICodeGeneratorService
{
    Task<string> GenerateDuAnCodeAsync(int? nam = null);
    Task<string> GenerateGoiThauCodeAsync(int? nam = null);
    Task<string> GenerateHopDongCodeAsync(int? nam = null);
    Task<string> GenerateDotThanhToanCodeAsync(Guid hopDongId, int? nam = null);
    Task<string> GenerateDoiTacCodeAsync(string nameOrShortName);
    Task<string> GenerateNguonVonCodeAsync(string nameOrAbbr);
    Task<string> GeneratePhanLoaiDuAnCodeAsync(string nameOrAbbr);
    Task<string> GenerateLoaiHopDongCodeAsync(string nameOrAbbr);
    Task<int> MigrateAllLegacyCodesAsync();
}
