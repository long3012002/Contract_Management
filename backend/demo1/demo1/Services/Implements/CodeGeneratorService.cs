using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace demo1.Services.Implements;

public class CodeGeneratorService : ICodeGeneratorService
{
    private readonly AppDbContext _context;

    public CodeGeneratorService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateDuAnCodeAsync(int? nam = null)
    {
        int targetYear = nam ?? DateTime.UtcNow.Year;
        string suffix = "DA";
        
        // Find existing codes for targetYear
        var existingCodes = await _context.DuAns
            .Where(d => !d.IsDeleted)
            .Select(d => d.Code)
            .ToListAsync();

        int maxSeq = 0;
        var pattern = new Regex($@"^(\d+)/{targetYear}/(DA|DAN|DATK)$", RegexOptions.IgnoreCase);

        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var match = pattern.Match(code.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
            {
                if (seq > maxSeq) maxSeq = seq;
            }
        }

        int nextSeq = maxSeq + 1;
        return $"{nextSeq:D3}/{targetYear}/{suffix}";
    }

    public async Task<string> GenerateGoiThauCodeAsync(int? nam = null)
    {
        int targetYear = nam ?? DateTime.UtcNow.Year;
        string suffix = "GT";

        var existingCodes = await _context.GoiThaus
            .Where(g => !g.IsDeleted)
            .Select(g => g.Code)
            .ToListAsync();

        int maxSeq = 0;
        var pattern = new Regex($@"^(\d+)/{targetYear}/{suffix}$", RegexOptions.IgnoreCase);

        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var match = pattern.Match(code.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
            {
                if (seq > maxSeq) maxSeq = seq;
            }
        }

        int nextSeq = maxSeq + 1;
        return $"{nextSeq:D3}/{targetYear}/{suffix}";
    }

    public async Task<string> GenerateHopDongCodeAsync(int? nam = null)
    {
        int targetYear = nam ?? DateTime.UtcNow.Year;
        string suffix = "HĐ";

        var existingCodes = await _context.HopDongs
            .Where(h => !h.IsDeleted)
            .Select(h => h.Code)
            .ToListAsync();

        int maxSeq = 0;
        var pattern = new Regex($@"^(\d+)/{targetYear}/(HĐ|HD)$", RegexOptions.IgnoreCase);

        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var match = pattern.Match(code.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
            {
                if (seq > maxSeq) maxSeq = seq;
            }
        }

        int nextSeq = maxSeq + 1;
        return $"{nextSeq:D3}/{targetYear}/{suffix}";
    }

    public async Task<string> GenerateDotThanhToanCodeAsync(Guid hopDongId, int? nam = null)
    {
        int targetYear = nam ?? DateTime.UtcNow.Year;
        var hopDong = await _context.HopDongs.FirstOrDefaultAsync(h => h.Id == hopDongId);
        
        string hdShortCode = "HD";
        if (hopDong != null && !string.IsNullOrWhiteSpace(hopDong.Code))
        {
            var match = Regex.Match(hopDong.Code.Trim(), @"^(\d+)/(\d{4})/(HĐ|HD)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                hdShortCode = $"HD{match.Groups[1].Value}";
            }
            else
            {
                // Custom contract code: sanitize slashes
                hdShortCode = hopDong.Code.Trim().Replace("/", "-").Replace(" ", "");
            }
        }

        var existingDotCodes = await _context.DotThanhToans
            .Where(d => d.HopDongId == hopDongId)
            .Select(d => d.Code)
            .ToListAsync();

        int maxDotSeq = existingDotCodes.Count;
        var dotPattern = new Regex($@"^(\d+)/{targetYear}/TT-", RegexOptions.IgnoreCase);

        foreach (var code in existingDotCodes)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var match = dotPattern.Match(code.Trim());
            if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
            {
                if (seq > maxDotSeq) maxDotSeq = seq;
            }
        }

        int nextDotSeq = maxDotSeq + 1;
        return $"{nextDotSeq:D2}/{targetYear}/TT-{hdShortCode}";
    }

    public Task<string> GenerateDoiTacCodeAsync(string nameOrShortName)
    {
        string clean = RemoveAccentsAndFormatting(nameOrShortName);
        if (clean.StartsWith("NT")) clean = clean.Substring(2).TrimStart('-', '_', ' ');
        return Task.FromResult(string.IsNullOrWhiteSpace(clean) ? "NT-DOITAC" : $"NT-{clean}");
    }

    public Task<string> GenerateNguonVonCodeAsync(string nameOrAbbr)
    {
        string clean = RemoveAccentsAndFormatting(nameOrAbbr);
        if (clean.StartsWith("NV")) clean = clean.Substring(2).TrimStart('-', '_', ' ');
        return Task.FromResult(string.IsNullOrWhiteSpace(clean) ? "NV-NGUONVON" : $"NV-{clean}");
    }

    public Task<string> GeneratePhanLoaiDuAnCodeAsync(string nameOrAbbr)
    {
        string clean = RemoveAccentsAndFormatting(nameOrAbbr);
        if (clean.StartsWith("PL")) clean = clean.Substring(2).TrimStart('-', '_', ' ');
        return Task.FromResult(string.IsNullOrWhiteSpace(clean) ? "PL-LOAIDA" : $"PL-{clean}");
    }

    public Task<string> GenerateLoaiHopDongCodeAsync(string nameOrAbbr)
    {
        string clean = RemoveAccentsAndFormatting(nameOrAbbr);
        if (clean.StartsWith("PLHD")) clean = clean.Substring(4).TrimStart('-', '_', ' ');
        return Task.FromResult(string.IsNullOrWhiteSpace(clean) ? "PLHD-LOAIHD" : $"PLHD-{clean}");
    }

    public async Task<int> MigrateAllLegacyCodesAsync()
    {
        int count = 0;

        // 1. Migrate DuAn
        var duAns = await _context.DuAns.Where(d => !d.IsDeleted).OrderBy(d => d.CreatedAt).ToListAsync();
        var duAnGrouped = duAns.GroupBy(d => d.CreatedAt.Year);
        foreach (var group in duAnGrouped)
        {
            int seq = 1;
            string suffix = "DA";
            foreach (var duAn in group)
            {
                duAn.Code = $"{seq:D3}/{group.Key}/{suffix}";
                seq++;
                count++;
            }
        }

        // 2. Migrate GoiThau
        var goiThaus = await _context.GoiThaus.Where(g => !g.IsDeleted).OrderBy(g => g.CreatedAt).ToListAsync();
        var goiThauGrouped = goiThaus.GroupBy(g => g.CreatedAt.Year);
        foreach (var group in goiThauGrouped)
        {
            int seq = 1;
            foreach (var gt in group)
            {
                gt.Code = $"{seq:D3}/{group.Key}/GT";
                seq++;
                count++;
            }
        }

        // 3. Migrate HopDong
        var hopDongs = await _context.HopDongs.Where(h => !h.IsDeleted).OrderBy(h => h.CreatedAt).ToListAsync();
        var hopDongGrouped = hopDongs.GroupBy(h => h.CreatedAt.Year);
        foreach (var group in hopDongGrouped)
        {
            int seq = 1;
            foreach (var hd in group)
            {
                hd.Code = $"{seq:D3}/{group.Key}/HĐ";
                seq++;
                count++;
            }
        }

        await _context.SaveChangesAsync();

        // 4. Migrate DotThanhToan
        var dotThanhToans = await _context.DotThanhToans.Include(d => d.HopDong).OrderBy(d => d.CreatedAt).ToListAsync();
        var dotGroupedByHopDong = dotThanhToans.GroupBy(d => d.HopDongId);
        foreach (var group in dotGroupedByHopDong)
        {
            int seq = 1;
            var hopDong = group.FirstOrDefault()?.HopDong;
            string hdShortCode = "HD";
            if (hopDong != null && !string.IsNullOrWhiteSpace(hopDong.Code))
            {
                var match = Regex.Match(hopDong.Code.Trim(), @"^(\d+)/(\d{4})/(HĐ|HD)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    hdShortCode = $"HD{match.Groups[1].Value}";
                }
                else
                {
                    hdShortCode = hopDong.Code.Trim().Replace("/", "-").Replace(" ", "");
                }
            }

            foreach (var dt in group)
            {
                int year = dt.CreatedAt.Year;
                dt.Code = $"{seq:D2}/{year}/TT-{hdShortCode}";
                seq++;
                count++;
            }
        }

        // 5. Migrate DoiTac
        var doiTacs = await _context.DoiTacs.Where(d => !d.IsDeleted).ToListAsync();
        foreach (var dt in doiTacs)
        {
            if (string.IsNullOrWhiteSpace(dt.Code) || !dt.Code.StartsWith("NT-"))
            {
                dt.Code = await GenerateDoiTacCodeAsync(string.IsNullOrWhiteSpace(dt.Name) ? dt.Code : dt.Name);
                count++;
            }
        }

        // 6. Migrate NguonVon
        var nguonVons = await _context.NguonVons.Where(n => !n.IsDeleted).ToListAsync();
        foreach (var nv in nguonVons)
        {
            if (string.IsNullOrWhiteSpace(nv.Code) || !nv.Code.StartsWith("NV-"))
            {
                nv.Code = await GenerateNguonVonCodeAsync(string.IsNullOrWhiteSpace(nv.Name) ? nv.Code : nv.Name);
                count++;
            }
        }

        // 7. Migrate PhanLoaiDuAn
        var phanLoaiDuAns = await _context.PhanLoaiDuAns.Where(p => !p.IsDeleted).ToListAsync();
        foreach (var pl in phanLoaiDuAns)
        {
            if (string.IsNullOrWhiteSpace(pl.Code) || !pl.Code.StartsWith("PL-"))
            {
                pl.Code = await GeneratePhanLoaiDuAnCodeAsync(string.IsNullOrWhiteSpace(pl.Name) ? pl.Code : pl.Name);
                count++;
            }
        }

        // 8. Migrate LoaiHopDong
        var loaiHopDongs = await _context.LoaiHopDongs.Where(l => !l.IsDeleted).ToListAsync();
        foreach (var lhd in loaiHopDongs)
        {
            if (string.IsNullOrWhiteSpace(lhd.Code) || !lhd.Code.StartsWith("PLHD-"))
            {
                lhd.Code = await GenerateLoaiHopDongCodeAsync(string.IsNullOrWhiteSpace(lhd.Name) ? lhd.Code : lhd.Name);
                count++;
            }
        }

        await _context.SaveChangesAsync();
        return count;
    }

    private static string RemoveAccentsAndFormatting(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var trimmed = input.Trim();

        // Check if multi-word string to generate acronym (e.g. "Bảo trì" -> "BT", "Ngân sách nhà nước" -> "NSNN")
        var words = trimmed.Split(new[] { ' ', '-', '_', '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            var acronym = new StringBuilder();
            foreach (var word in words)
            {
                var cleanWord = RemoveAccentsCharByChar(word);
                if (!string.IsNullOrEmpty(cleanWord))
                {
                    acronym.Append(cleanWord[0]);
                }
            }
            return acronym.ToString().ToUpperInvariant();
        }

        return RemoveAccentsCharByChar(trimmed).ToUpperInvariant();
    }

    private static string RemoveAccentsCharByChar(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        string str = input.Replace("Đ", "D").Replace("đ", "d");
        string normalizedString = str.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (char c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
