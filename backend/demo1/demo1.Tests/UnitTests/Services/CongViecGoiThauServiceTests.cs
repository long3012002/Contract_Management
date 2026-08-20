using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class CongViecGoiThauServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly CongViecReminderHangfireService _reminderService;
        private readonly CongViecGoiThauService _congViecService;

        public CongViecGoiThauServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            var configDict = new Dictionary<string, string?>
            {
                {"Task:ConfirmationDeadlineHours", "48"}
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("admin");

            var mockClientProxy = new Mock<IClientProxy>();
            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
            _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            var mockReminderLogger = new Mock<ILogger<CongViecReminderHangfireService>>();
            _reminderService = new CongViecReminderHangfireService(_dbContext, _mockHubContext.Object, mockReminderLogger.Object, config);

            try
            {
                var mockStorage = new Mock<Hangfire.JobStorage>();
                Hangfire.JobStorage.Current = mockStorage.Object;
            }
            catch { }

            _congViecService = new CongViecGoiThauService(
                _dbContext,
                _mapper,
                _mockHubContext.Object,
                _reminderService,
                config,
                _mockCurrentUserService.Object
            );
        }

        [Fact]
        public async Task TC40_TC41_CreateAsync_Should_Create_Task_And_Add_RelatedUsers()
        {
            // Arrange
            var goiThau = new GoiThau { Id = Guid.NewGuid(), Code = "GT-01", Name = "Gói thầu test công việc" };
            _dbContext.GoiThaus.Add(goiThau);

            var userA = new User { Id = Guid.NewGuid(), Username = "userA", FullName = "Nguyen Van A" };
            var userB = new User { Id = Guid.NewGuid(), Username = "userB", FullName = "Tran Van B" };
            _dbContext.Users.AddRange(userA, userB);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateCongViecGoiThauDto
            {
                GoiThauId = goiThau.Id,
                Code = "CV-01",
                TenTaiLieu = "Soạn thảo HSMT",
                Description = "Chi tiết công việc",
                NguoiLienQuanIds = new List<Guid> { userB.Id }
            };

            // Act
            var result = await _congViecService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.TenTaiLieu.Should().Be("Soạn thảo HSMT");

            var relatedUsers = _dbContext.CongViecNguoiLienQuans.Where(r => r.CongViecGoiThauId == result.Id).ToList();
            relatedUsers.Should().NotBeEmpty();
        }

        [Fact]
        public async Task TC42_UpdateStatus_Should_Change_State_To_Completed()
        {
            // Arrange
            var goiThau = new GoiThau { Id = Guid.NewGuid(), Code = "GT-STATUS", Name = "Gói thầu test status" };
            _dbContext.GoiThaus.Add(goiThau);

            var task = new CongViecGoiThau
            {
                Id = Guid.NewGuid(),
                GoiThauId = goiThau.Id,
                Code = "CV-TEST",
                TenTaiLieu = "Thực hiện kiểm thử",
                TinhTrang = "Đang thực hiện"
            };
            _dbContext.CongViecGoiThaus.Add(task);
            await _dbContext.SaveChangesAsync();

            // Act
            task.TinhTrang = "Hoàn thành";
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbTask = await _dbContext.CongViecGoiThaus.FindAsync(task.Id);
            dbTask!.TinhTrang.Should().Be("Hoàn thành");
        }

        [Fact]
        public async Task TC44_TC45_Comment_And_Mention_User_In_Task()
        {
            // Arrange
            var goiThau = new GoiThau { Id = Guid.NewGuid(), Code = "GT-COMMENT", Name = "Gói thầu comment" };
            _dbContext.GoiThaus.Add(goiThau);

            var task = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = goiThau.Id, Code = "CV-MENTION", TenTaiLieu = "Task check mention" };
            var author = new User { Id = Guid.NewGuid(), Username = "author" };
            var mentionedUser = new User { Id = Guid.NewGuid(), Username = "LeDucAnh", FullName = "Lê Đức Anh" };
            _dbContext.CongViecGoiThaus.Add(task);
            _dbContext.Users.AddRange(author, mentionedUser);
            await _dbContext.SaveChangesAsync();

            var comment = new CommentCongViecGoiThau
            {
                Id = Guid.NewGuid(),
                CongViecGoiThauId = task.Id,
                UserId = author.Id,
                Content = "@LeDucAnh xin vui lòng kiểm tra lại điều khoản HSMT",
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.CommentCongViecGoiThaus.Add(comment);

            var mention = new CommentMention
            {
                Id = Guid.NewGuid(),
                CommentId = comment.Id,
                MentionedUserId = mentionedUser.Id
            };
            _dbContext.CommentMentions.Add(mention);
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbComment = await _dbContext.CommentCongViecGoiThaus.FindAsync(comment.Id);
            dbComment.Should().NotBeNull();
            dbComment!.Content.Should().Contain("@LeDucAnh");

            var mentions = _dbContext.CommentMentions.Where(m => m.CommentId == comment.Id).ToList();
            mentions.Should().HaveCount(1);
            mentions[0].MentionedUserId.Should().Be(mentionedUser.Id);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
