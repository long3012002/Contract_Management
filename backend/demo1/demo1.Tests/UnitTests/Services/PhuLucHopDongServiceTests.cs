using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services;

public class PhuLucHopDongServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly PhuLucHopDongService _phuLucService;
    private readonly HopDongService _hopDongService;

    public PhuLucHopDongServiceTests()
    {
        _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
        var serviceProvider = services.BuildServiceProvider();
        _mapper = serviceProvider.GetRequiredService<IMapper>();

        var mockLogger = new Mock<ILogger<HopDongService>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(x => x.GetUsername()).Returns("admin");

        var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin", IsSystemAdmin = true, IsActive = true };
        _dbContext.Users.Add(adminUser);
        _dbContext.SaveChanges();

        _phuLucService = new PhuLucHopDongService(_dbContext);
        _hopDongService = new HopDongService(_dbContext, _mapper, mockLogger.Object, mockCurrentUserService.Object);
    }

    [Fact]
    public async Task CreatePhuLuc_ValueAdjustment_Should_Update_Contract_TotalValue()
    {
        // Arrange: Contract worth 2 billion VND
        var contract = new HopDong
        {
            Id = Guid.NewGuid(),
            Code = "HD-2026/001",
            Name = "Hợp đồng phần mềm",
            GiaTriHopDong = 2000000000m,
            NgayHieuLuc = DateTime.UtcNow
        };
        _dbContext.HopDongs.Add(contract);
        await _dbContext.SaveChangesAsync();

        // Act: Create addendum with +200M adjustment
        var createDto = new CreatePhuLucHopDongDto
        {
            HopDongId = contract.Id,
            SoPhuLuc = "PL01/HD-2026/001",
            TenPhuLuc = "Phụ lục bổ sung hạng mục A",
            LoaiPhuLuc = LoaiPhuLuc.DieuChinhGiaTri,
            TrangThai = TrangThaiPhuLuc.Active,
            GiaTriDieuChinh = 200000000m,
            NgayKy = DateTime.UtcNow,
            NgayHieuLuc = DateTime.UtcNow
        };

        var created = await _phuLucService.CreateAsync(createDto);
        created.Should().NotBeNull();
        created.GiaTriDieuChinh.Should().Be(200000000m);

        // Assert: Query contract and verify TongGiaTriHienTai is 2.2 billion
        var hopDongDto = await _hopDongService.GetByIdAsync(contract.Id);
        hopDongDto.Should().NotBeNull();
        hopDongDto!.TongGiaTriPhuLucActive.Should().Be(200000000m);
        hopDongDto.TongGiaTriHienTai.Should().Be(2200000000m);
    }

    [Fact]
    public async Task CancelPhuLuc_Should_Remove_Value_From_Contract_TotalValue()
    {
        // Arrange
        var contract = new HopDong
        {
            Id = Guid.NewGuid(),
            Code = "HD-2026/002",
            Name = "Hợp đồng hạ tầng",
            GiaTriHopDong = 1500000000m
        };
        _dbContext.HopDongs.Add(contract);

        var phuLuc = new PhuLucHopDong
        {
            Id = Guid.NewGuid(),
            HopDongId = contract.Id,
            Code = "PL01",
            Name = "Phụ lục tăng giá trị",
            SoPhuLuc = "PL01",
            TenPhuLuc = "Phụ lục tăng giá trị",
            LoaiPhuLuc = LoaiPhuLuc.DieuChinhGiaTri,
            TrangThai = TrangThaiPhuLuc.Active,
            GiaTriDieuChinh = 300000000m
        };
        _dbContext.PhuLucHopDongs.Add(phuLuc);
        await _dbContext.SaveChangesAsync();

        // Verify active value
        var dtoActive = await _hopDongService.GetByIdAsync(contract.Id);
        dtoActive!.TongGiaTriHienTai.Should().Be(1800000000m);

        // Act: Update status to Cancelled
        await _phuLucService.UpdateStatusAsync(phuLuc.Id, TrangThaiPhuLuc.Cancelled);

        // Assert: TongGiaTriHienTai reverts to 1.5 billion
        var dtoCancelled = await _hopDongService.GetByIdAsync(contract.Id);
        dtoCancelled!.TongGiaTriPhuLucActive.Should().Be(0m);
        dtoCancelled.TongGiaTriHienTai.Should().Be(1500000000m);
    }

    [Fact]
    public async Task CreatePhuLuc_Extension_Should_Update_ExpiredDateHienTai()
    {
        // Arrange
        var oldExpired = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var contract = new HopDong
        {
            Id = Guid.NewGuid(),
            Code = "HD-2026/003",
            Name = "Hợp đồng dịch vụ IT",
            GiaTriHopDong = 500000000m,
            ExpiredDate = oldExpired
        };
        _dbContext.HopDongs.Add(contract);
        await _dbContext.SaveChangesAsync();

        var newExpired = new DateTime(2027, 6, 30, 0, 0, 0, DateTimeKind.Utc);
        var createDto = new CreatePhuLucHopDongDto
        {
            HopDongId = contract.Id,
            SoPhuLuc = "PL02",
            TenPhuLuc = "Phụ lục gia hạn 6 tháng",
            LoaiPhuLuc = LoaiPhuLuc.GiaHanThoiGian,
            TrangThai = TrangThaiPhuLuc.Active,
            ExpiredDateMoi = newExpired,
            NgayKy = DateTime.UtcNow
        };

        await _phuLucService.CreateAsync(createDto);

        // Assert: ExpiredDateHienTai should be updated to 2027-06-30
        var hopDongDto = await _hopDongService.GetByIdAsync(contract.Id);
        hopDongDto!.ExpiredDateHienTai.Should().Be(newExpired);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
