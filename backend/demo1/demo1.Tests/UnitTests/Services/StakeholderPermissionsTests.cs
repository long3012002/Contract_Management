using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Hubs;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class StakeholderPermissionsTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly CongViecReminderHangfireService _reminderService;
        private readonly CongViecGoiThauService _congViecService;
        private readonly GoiThauService _goiThauService;
        private readonly HopDongService _hopDongService;

        public StakeholderPermissionsTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockConfig = new Mock<IConfiguration>();

            var mockClientProxy = new Mock<IClientProxy>();
            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            var mockReminderLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CongViecReminderHangfireService>>();
            var mockGoiThauLogger = new Mock<Microsoft.Extensions.Logging.ILogger<GoiThauService>>();
            var mockHopDongLogger = new Mock<Microsoft.Extensions.Logging.ILogger<HopDongService>>();

            _reminderService = new CongViecReminderHangfireService(_dbContext, _mockHubContext.Object, mockReminderLogger.Object, _mockConfig.Object);

            _congViecService = new CongViecGoiThauService(
                _dbContext,
                _mapper,
                _mockHubContext.Object,
                _reminderService,
                _mockConfig.Object,
                _mockCurrentUserService.Object
            );

            _goiThauService = new GoiThauService(_dbContext, _mapper, mockGoiThauLogger.Object, _mockCurrentUserService.Object);
            _hopDongService = new HopDongService(_dbContext, _mapper, mockHopDongLogger.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Stakeholder_Can_Only_View_Tagged_Task_And_Its_Package_And_Contract()
        {
            // Arrange
            var projectOwner = new User { Id = Guid.NewGuid(), Username = "owner", IsActive = true };
            var stakeholder = new User { Id = Guid.NewGuid(), Username = "stakeholder", IsActive = true };
            _dbContext.Users.AddRange(projectOwner, stakeholder);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-01", Name = "Dự án 1", CreatedByUserId = projectOwner.Id };
            _dbContext.DuAns.Add(project);

            var goiThau1 = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-01", Name = "Gói thầu 1" };
            var goiThau2 = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-02", Name = "Gói thầu 2" };
            _dbContext.GoiThaus.AddRange(goiThau1, goiThau2);

            var hopDong1 = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau1.Id, Code = "HD-01", Name = "Hợp đồng 1" };
            var hopDong2 = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau2.Id, Code = "HD-02", Name = "Hợp đồng 2" };
            _dbContext.HopDongs.AddRange(hopDong1, hopDong2);

            var task1 = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = goiThau1.Id, Code = "CV-01", TenTaiLieu = "Công việc 1 (Tagged)" };
            var task2 = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = goiThau1.Id, Code = "CV-02", TenTaiLieu = "Công việc 2 (Untagged)" };
            _dbContext.CongViecGoiThaus.AddRange(task1, task2);

            var tag = new CongViecNguoiLienQuan { Id = Guid.NewGuid(), CongViecGoiThauId = task1.Id, UserId = stakeholder.Id };
            _dbContext.CongViecNguoiLienQuans.Add(tag);

            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("stakeholder");

            // Act & Assert 1: Tasks listing under GoiThau 1
            var tasks = await _congViecService.GetByParentIdAsync(goiThau1.Id);
            tasks.Should().HaveCount(1);
            tasks.First().Id.Should().Be(task1.Id);

            // Act & Assert 2: GoiThau listing
            var packages = await _goiThauService.GetAllAsync(new GoiThauFilterDto { Page = 1, PageSize = 10 });
            packages.Items.Should().HaveCount(1);
            packages.Items.First().Id.Should().Be(goiThau1.Id);

            // Act & Assert 3: HopDong listing
            var contracts = await _hopDongService.GetAllAsync(new HopDongFilterDto { Page = 1, PageSize = 10 });
            contracts.Items.Should().HaveCount(1);
            contracts.Items.First().Id.Should().Be(hopDong1.Id);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
