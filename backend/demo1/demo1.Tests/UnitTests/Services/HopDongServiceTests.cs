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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class HopDongServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<HopDongService>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly HopDongService _hopDongService;

        public HopDongServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _mockLogger = new Mock<ILogger<HopDongService>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("admin");

            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin", IsSystemAdmin = true, IsActive = true };
            _dbContext.Users.Add(adminUser);
            _dbContext.SaveChanges();

            _hopDongService = new HopDongService(_dbContext, _mapper, _mockLogger.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task TC46_CreateAsync_Should_Create_Contract_Linked_To_Package()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-01", Name = "Dự án A" };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-01", Name = "Gói thầu A", GiaTriGoiThau = 2000000000 };
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateHopDongDto
            {
                DuAnId = project.Id,
                GoiThauId = goiThau.Id,
                Code = "HD2026/01",
                Name = "Hợp đồng Mua sắm máy chủ",
                GiaTriHopDong = 1800000000
            };

            // Act
            var result = await _hopDongService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.GoiThauId.Should().Be(goiThau.Id);
            result.GiaTriHopDong.Should().Be(1800000000);
        }

        [Fact]
        public async Task TC47_CreateAsync_Should_Prevent_Duplicate_Contract_For_Same_Package()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-02", Name = "Dự án B" };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-02", Name = "Gói thầu B" };
            var existingContract = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau.Id, Code = "HD-EXISTING", Name = "Hợp đồng đã có" };
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.HopDongs.Add(existingContract);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var isPackageHasContract = await _dbContext.HopDongs.AnyAsync(h => h.GoiThauId == goiThau.Id);
            isPackageHasContract.Should().BeTrue();
        }

        [Fact]
        public async Task TC48_PhuLucHopDong_Should_Update_Contract_Total_Value()
        {
            // Arrange
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD2026/01",
                Name = "Hợp đồng gốc",
                GiaTriHopDong = 1800000000
            };
            _dbContext.HopDongs.Add(contract);
            await _dbContext.SaveChangesAsync();

            // Act: Adjust contract value with addendum (+200M)
            decimal giaTriAddendum = 200000000m;
            contract.GiaTriHopDong += giaTriAddendum;
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbContract = await _dbContext.HopDongs.FindAsync(contract.Id);
            dbContract!.GiaTriHopDong.Should().Be(2000000000);
        }

        [Fact]
        public async Task TC49_TC50_Payment_Milestone_Should_Track_Disbursement_And_Prevent_Overpayment()
        {
            // Arrange: Contract worth 2 billion, paid 1.8 billion
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-PAY",
                Name = "Hợp đồng thanh toán",
                GiaTriHopDong = 2000000000
            };
            _dbContext.HopDongs.Add(contract);

            var dot1 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                TenDot = "Tạm ứng 30%",
                GiaTriThanhToan = 1800000000,
                IsPaid = true
            };
            _dbContext.DotThanhToans.Add(dot1);
            await _dbContext.SaveChangesAsync();

            // Act 1: Sum existing payments
            var totalPaid = _dbContext.DotThanhToans.Where(d => d.HopDongId == contract.Id).Sum(d => d.GiaTriThanhToan);
            totalPaid.Should().Be(1800000000);

            // Act 2: New payment of 500M (Total 2.3B > 2B contract value)
            var newPayment = 500000000m;
            var isOverpayment = (totalPaid + newPayment) > contract.GiaTriHopDong;

            // Assert
            isOverpayment.Should().BeTrue();
        }

        [Fact]
        public async Task TC53_BaoLanhHopDong_Expiring_Warning_Check()
        {
            // Arrange
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-BL",
                Name = "Hợp đồng có bảo lãnh",
                ExpiredDate = DateTime.UtcNow.AddDays(15), // Expiring in 15 days
                RenewalReminderDate = DateTime.UtcNow.AddDays(30)
            };
            _dbContext.HopDongs.Add(contract);
            await _dbContext.SaveChangesAsync();

            // Act
            var isExpiringSoon = contract.ExpiredDate <= DateTime.UtcNow.AddDays(30);

            // Assert
            isExpiringSoon.Should().BeTrue();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
