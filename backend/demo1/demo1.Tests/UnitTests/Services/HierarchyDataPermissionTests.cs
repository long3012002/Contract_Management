using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using demo1.Hubs;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class HierarchyDataPermissionTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<ILogger<GoiThauService>> _mockGoiThauLogger;
        private readonly Mock<ILogger<HopDongService>> _mockHopDongLogger;

        private readonly ChucVu _managerChucVu;
        private readonly ChucVu _specialistChucVu;

        private readonly User _managerUser;
        private readonly User _specialistUser;

        public HierarchyDataPermissionTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();

            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockGoiThauLogger = new Mock<ILogger<GoiThauService>>();
            _mockHopDongLogger = new Mock<ILogger<HopDongService>>();

            // Setup ChucVu: Manager = Level 2, Specialist = Level 3
            _managerChucVu = new ChucVu { Id = Guid.NewGuid(), Code = "TP", TenChucVu = "Trưởng phòng", Level = 2 };
            _specialistChucVu = new ChucVu { Id = Guid.NewGuid(), Code = "CV", TenChucVu = "Chuyên viên", Level = 3 };
            _dbContext.ChucVus.AddRange(_managerChucVu, _specialistChucVu);

            // Setup Users
            _managerUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "manager_tp",
                FullName = "Trưởng phòng A",
                IdChucVu = _managerChucVu.Id,
                IsActive = true,
                IsSystemAdmin = false
            };
            _specialistUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "specialist_cv",
                FullName = "Chuyên viên B",
                IdChucVu = _specialistChucVu.Id,
                IsActive = true,
                IsSystemAdmin = false
            };
            _dbContext.Users.AddRange(_managerUser, _specialistUser);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Manager_With_Higher_Position_Should_See_Projects_Created_By_Subordinate()
        {
            // Arrange: Specialist creates project DA-SUBORDINATE
            var projectSubordinate = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-SUBORDINATE",
                Name = "Dự án do Chuyên viên tạo",
                LoaiDuAn = 1,
                CreatedByUserId = _specialistUser.Id,
                ChuDuAnId = _specialistUser.Id
            };
            _dbContext.DuAns.Add(projectSubordinate);
            await _dbContext.SaveChangesAsync();

            var duAnService = new DuAnService(_dbContext, _mapper, _mockCurrentUserService.Object, _mockHubContext.Object);

            // Act 1: Manager queries projects
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns(_managerUser.Username);
            var managerResult = await duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });

            // Assert 1: Manager should SEE the project created by Specialist
            managerResult.Items.Should().ContainSingle(p => p.Id == projectSubordinate.Id);

            // Act 2: Specialist queries projects
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns(_specialistUser.Username);
            var specialistResult = await duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });

            // Assert 2: Specialist should SEE their own project
            specialistResult.Items.Should().ContainSingle(p => p.Id == projectSubordinate.Id);
        }

        [Fact]
        public async Task Subordinate_With_Lower_Position_Should_NOT_See_Projects_Created_By_Manager()
        {
            // Arrange: Manager creates project DA-MANAGER
            var projectManager = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-MANAGER",
                Name = "Dự án do Trưởng phòng tạo",
                LoaiDuAn = 1,
                CreatedByUserId = _managerUser.Id,
                ChuDuAnId = _managerUser.Id
            };
            _dbContext.DuAns.Add(projectManager);
            await _dbContext.SaveChangesAsync();

            var duAnService = new DuAnService(_dbContext, _mapper, _mockCurrentUserService.Object, _mockHubContext.Object);

            // Act: Specialist queries projects
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns(_specialistUser.Username);
            var specialistResult = await duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });

            // Assert: Specialist should NOT see the project created by Manager
            specialistResult.Items.Should().NotContain(p => p.Id == projectManager.Id);
        }

        [Fact]
        public async Task Manager_Should_See_Packages_And_Contracts_Of_Subordinate_Project()
        {
            // Arrange
            var projectSubordinate = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-SUB-2",
                Name = "Dự án Gói thầu cấp dưới",
                LoaiDuAn = 1,
                CreatedByUserId = _specialistUser.Id
            };
            var goiThau = new GoiThau
            {
                Id = Guid.NewGuid(),
                DuAnId = projectSubordinate.Id,
                Name = "Gói thầu của cấp dưới"
            };
            var hopDong = new HopDong
            {
                Id = Guid.NewGuid(),
                DuAnId = projectSubordinate.Id,
                GoiThauId = goiThau.Id,
                Name = "Hợp đồng của cấp dưới"
            };
            _dbContext.DuAns.Add(projectSubordinate);
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.HopDongs.Add(hopDong);
            await _dbContext.SaveChangesAsync();

            var goiThauService = new GoiThauService(_dbContext, _mapper, _mockGoiThauLogger.Object, _mockCurrentUserService.Object);
            var hopDongService = new HopDongService(_dbContext, _mapper, _mockHopDongLogger.Object, _mockCurrentUserService.Object);

            // Act
            _mockCurrentUserService.Setup(c => c.GetUsername()).Returns(_managerUser.Username);

            var goiThauResult = await goiThauService.GetAllAsync(new GoiThauFilterDto { Page = 1, PageSize = 10 });
            var hopDongResult = await hopDongService.GetAllAsync(new HopDongFilterDto { Page = 1, PageSize = 10 });

            // Assert
            goiThauResult.Items.Should().ContainSingle(gt => gt.Id == goiThau.Id);
            hopDongResult.Items.Should().ContainSingle(hd => hd.Id == hopDong.Id);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
