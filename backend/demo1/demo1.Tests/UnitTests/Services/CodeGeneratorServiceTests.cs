using System;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using Xunit;

namespace demo1.Tests.UnitTests.Services;

public class CodeGeneratorServiceTests
{
    private readonly AppDbContext _context;
    private readonly CodeGeneratorService _service;

    public CodeGeneratorServiceTests()
    {
        _context = DbContextTestHelper.CreateSqliteInMemoryDbContext();
        _service = new CodeGeneratorService(_context);
    }

    [Fact]
    public async Task GenerateDuAnCode_SourceProject_ReturnsFormattedCode()
    {
        var year = DateTime.UtcNow.Year;
        var code1 = await _service.GenerateDuAnCodeAsync(1);
        Assert.Equal($"001/{year}/DAN", code1);

        _context.DuAns.Add(new DuAn { Code = code1, LoaiDuAn = 1, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var code2 = await _service.GenerateDuAnCodeAsync(1);
        Assert.Equal($"002/{year}/DAN", code2);
    }

    [Fact]
    public async Task GenerateDuAnCode_ImplementationProject_ReturnsFormattedCode()
    {
        var year = DateTime.UtcNow.Year;
        var code1 = await _service.GenerateDuAnCodeAsync(2);
        Assert.Equal($"001/{year}/DATK", code1);
    }

    [Fact]
    public async Task GenerateGoiThauCode_ReturnsFormattedCode()
    {
        var year = DateTime.UtcNow.Year;
        var code = await _service.GenerateGoiThauCodeAsync();
        Assert.Equal($"001/{year}/GT", code);
    }

    [Fact]
    public async Task GenerateHopDongCode_ReturnsFormattedCode()
    {
        var year = DateTime.UtcNow.Year;
        var code = await _service.GenerateHopDongCodeAsync();
        Assert.Equal($"001/{year}/HĐ", code);
    }

    [Fact]
    public async Task GenerateDotThanhToanCode_ReturnsFormattedCode()
    {
        var year = DateTime.UtcNow.Year;
        var hopDongId = Guid.NewGuid();
        _context.HopDongs.Add(new HopDong { Id = hopDongId, Code = $"017/{year}/HĐ", Name = "Hợp đồng 017" });
        await _context.SaveChangesAsync();

        var code1 = await _service.GenerateDotThanhToanCodeAsync(hopDongId);
        Assert.Equal($"01/{year}/TT-HD017", code1);
    }

    [Fact]
    public async Task GenerateDoiTacCode_ReturnsFormattedCode()
    {
        var code = await _service.GenerateDoiTacCodeAsync("Công ty CMC");
        Assert.Equal("NT-CONGTYCMC", code);
    }

    [Fact]
    public async Task GenerateNguonVonCode_ReturnsFormattedCode()
    {
        var code = await _service.GenerateNguonVonCodeAsync("Ngân sách nhà nước");
        Assert.Equal("NV-NGANSACHNHANUOC", code);
    }
}
