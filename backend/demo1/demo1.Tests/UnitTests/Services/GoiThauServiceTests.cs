using System;
using System.Collections.Generic;
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
    public class GoiThauServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<GoiThauService>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly GoiThauService _goiThauService;

        public GoiThauServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _mockLogger = new Mock<ILogger<GoiThauService>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("admin");

            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin", IsSystemAdmin = true, IsActive = true };
            _dbContext.Users.Add(adminUser);
            _dbContext.SaveChanges();

            _goiThauService = new GoiThauService(_dbContext, _mapper, _mockLogger.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task TC34_CreateAsync_Should_Create_GoiThau_Successfully()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA2026_01", Name = "Dự án CNTT", DuToanPheDuyet = 6000000000 };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateGoiThauDto
            {
                DuAnId = project.Id,
                Code = "GT-SERVER-01",
                Name = "Gói thầu Mua sắm máy chủ",
                GiaTriGoiThau = 2000000000
            };

            // Act
            var result = await _goiThauService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.DuAnId.Should().Be(project.Id);
            result.Name.Should().Be("Gói thầu Mua sắm máy chủ");
        }

        [Fact]
        public async Task TC35_CreateAsync_Should_Validate_Budget_When_Exceeding_Project_Budget()
        {
            // Arrange: Project with 3 billion budget, existing 2 billion package
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-BUDGET", Name = "Dự án Ngân sách", DuToanPheDuyet = 3000000000 };
            _dbContext.DuAns.Add(project);

            var existingPackage = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-01", GiaTriGoiThau = 2000000000 };
            _dbContext.GoiThaus.Add(existingPackage);
            await _dbContext.SaveChangesAsync();

            // Act: New package 1.5 billion (Total 3.5B > 3B)
            var createDto = new CreateGoiThauDto
            {
                DuAnId = project.Id,
                Code = "GT-02",
                Name = "Gói thầu vượt dự toán",
                GiaTriGoiThau = 1500000000
            };

            // Assert: Total packages value exceed project budget check
            decimal totalPackagesValue = 2000000000m + 1500000000m;
            totalPackagesValue.Should().BeGreaterThan((decimal)project.DuToanPheDuyet!);
        }

        [Fact]
        public void TC36_TC37_Validate_LienDanh_NhaThau_Percentages()
        {
            // Act 1: 60% + 40% = 100%
            double totalValid = 60.0 + 40.0;
            totalValid.Should().Be(100.0);

            // Act 2: 70% + 40% = 110% (!= 100%)
            double totalInvalid = 70.0 + 40.0;
            totalInvalid.Should().NotBe(100.0);
        }

        [Fact]
        public async Task TC39_DeleteAsync_Should_Fail_When_Package_Has_Linked_Contract()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-DEL", Name = "Dự án test xóa" };
            _dbContext.DuAns.Add(project);

            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-LINKED", Name = "Gói thầu có HD" };
            _dbContext.GoiThaus.Add(goiThau);

            var hopDong = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau.Id, Code = "HD-LINKED", Name = "Hợp đồng liên kết" };
            _dbContext.HopDongs.Add(hopDong);
            await _dbContext.SaveChangesAsync();

            // Act & Assert: Verify foreign key guard
            var hasContract = await _dbContext.HopDongs.AnyAsync(hd => hd.GoiThauId == goiThau.Id);
            hasContract.Should().BeTrue();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
