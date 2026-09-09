using System;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using demo1.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class DuAnServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly DuAnService _duAnService;

        public DuAnServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("test_admin");

            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            _mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);
            mockClients.Setup(x => x.User(It.IsAny<string>())).Returns(mockClientProxy.Object);

            _duAnService = new DuAnService(_dbContext, _mapper, _mockCurrentUserService.Object, _mockHubContext.Object);
        }

        [Fact]
        public async Task CreateAsync_Should_Save_Project_Successfully_When_Valid()
        {
            var createDto = new CreateDuAnDto
            {
                Code = "DA-TEST-001",
                Name = "Dự án Thử nghiệm tự động",
                LoaiDuAn = 1,
                Description = "Mô tả dự án kiểm thử"
            };

            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var result = await _duAnService.CreateAsync(createDto);

            result.Should().NotBeNull();
            result.Code.Should().Be("DA-TEST-001");
            result.Name.Should().Be("Dự án Thử nghiệm tự động");

            var dbProject = await _dbContext.DuAns.FindAsync(result.Id);
            dbProject.Should().NotBeNull();
            dbProject!.Code.Should().Be("DA-TEST-001");
        }

        [Fact]
        public async Task GetAllAsync_Should_Return_Only_Permitted_Projects_For_Normal_User()
        {
            var user = new User { Id = Guid.NewGuid(), Username = "normal_user", IsSystemAdmin = false, IsActive = true };
            var otherUser = new User { Id = Guid.NewGuid(), Username = "other_user", IsSystemAdmin = false, IsActive = true };
            _dbContext.Users.AddRange(user, otherUser);

            var myProject = new DuAn { Id = Guid.NewGuid(), Code = "MY-PROJ", Name = "Dự án của tôi", CreatedByUserId = user.Id, CreatedAt = DateTime.UtcNow };
            var otherProject = new DuAn { Id = Guid.NewGuid(), Code = "OTHER-PROJ", Name = "Dự án người khác", CreatedByUserId = otherUser.Id, CreatedAt = DateTime.UtcNow };
            _dbContext.DuAns.AddRange(myProject, otherProject);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("normal_user");

            var result = await _duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });

            result.Items.Should().HaveCount(1);
            result.Items[0].Code.Should().Be("MY-PROJ");
        }

        [Fact]
        public async Task GetAllAsync_Should_Not_Return_Project_When_User_Is_Only_Task_Stakeholder()
        {
            var stakeholderUser = new User { Id = Guid.NewGuid(), Username = "stakeholder_user", IsSystemAdmin = false, IsActive = true };
            var projectOwner = new User { Id = Guid.NewGuid(), Username = "project_owner", IsSystemAdmin = false, IsActive = true };
            _dbContext.Users.AddRange(stakeholderUser, projectOwner);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "PROJ-STAKEHOLDER", Name = "Dự án có người liên quan", CreatedByUserId = projectOwner.Id, CreatedAt = DateTime.UtcNow };
            _dbContext.DuAns.Add(project);

            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-01", Name = "Gói thầu 1" };
            _dbContext.GoiThaus.Add(goiThau);

            var congViec = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = goiThau.Id, TenTaiLieu = "Công việc mẫu" };
            _dbContext.CongViecGoiThaus.Add(congViec);

            var nq = new CongViecNguoiLienQuan { Id = Guid.NewGuid(), CongViecGoiThauId = congViec.Id, UserId = stakeholderUser.Id };
            _dbContext.CongViecNguoiLienQuans.Add(nq);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("stakeholder_user");

            var result = await _duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateAsync_Should_Save_PhanKyVons_And_Calculate_Percentages()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "DA-PHANKY-001",
                Name = "Dự án có phân kỳ vốn",
                DuToanPheDuyet = 1000000000m,
                PhanKyVons = new List<CreateDuAnPhanKyVonDto>
                {
                    new CreateDuAnPhanKyVonDto { Nam = 2025, SoTienPhanKy = 400000000m, GhiChu = "Đợt 1" },
                    new CreateDuAnPhanKyVonDto { Nam = 2026, SoTienPhanKy = 600000000m, GhiChu = "Đợt 2" }
                }
            };

            var result = await _duAnService.CreateAsync(createDto);

            result.Should().NotBeNull();
            result.PhanKyVons.Should().HaveCount(2);
            result.PhanKyVons[0].Nam.Should().Be(2025);
            result.PhanKyVons[0].SoTienPhanKy.Should().Be(400000000m);
            result.PhanKyVons[0].TyLePercent.Should().Be(40.00m);

            result.PhanKyVons[1].Nam.Should().Be(2026);
            result.PhanKyVons[1].SoTienPhanKy.Should().Be(600000000m);
            result.PhanKyVons[1].TyLePercent.Should().Be(60.00m);
        }

        [Fact]
        public async Task UpdateAsync_Should_Upsert_And_Remove_PhanKyVons()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "DA-PHANKY-002",
                Name = "Dự án phân kỳ vốn update",
                DuToanPheDuyet = 1000000000m,
                PhanKyVons = new List<CreateDuAnPhanKyVonDto>
                {
                    new CreateDuAnPhanKyVonDto { Nam = 2025, SoTienPhanKy = 500000000m },
                    new CreateDuAnPhanKyVonDto { Nam = 2026, SoTienPhanKy = 500000000m }
                }
            };
            var created = await _duAnService.CreateAsync(createDto);

            var updateDto = new UpdateDuAnDto
            {
                Code = "DA-PHANKY-002",
                Name = "Dự án phân kỳ vốn update (đã sửa)",
                DuToanPheDuyet = 1000000000m,
                PhanKyVons = new List<CreateDuAnPhanKyVonDto>
                {
                    new CreateDuAnPhanKyVonDto { Nam = 2025, SoTienPhanKy = 300000000m }, // Updated
                    new CreateDuAnPhanKyVonDto { Nam = 2027, SoTienPhanKy = 700000000m }  // New year 2027, 2026 removed
                }
            };

            var updateSuccess = await _duAnService.UpdateAsync(created.Id, updateDto);
            updateSuccess.Should().BeTrue();

            var updatedProject = await _duAnService.GetByIdAsync(created.Id);
            updatedProject.Should().NotBeNull();
            updatedProject!.PhanKyVons.Should().HaveCount(2);
            updatedProject.PhanKyVons[0].Nam.Should().Be(2025);
            updatedProject.PhanKyVons[0].SoTienPhanKy.Should().Be(300000000m);
            updatedProject.PhanKyVons[0].TyLePercent.Should().Be(30.00m);

            updatedProject.PhanKyVons[1].Nam.Should().Be(2027);
            updatedProject.PhanKyVons[1].SoTienPhanKy.Should().Be(700000000m);
            updatedProject.PhanKyVons[1].TyLePercent.Should().Be(70.00m);
        }

        [Fact]
        public async Task GetByIdAsync_And_GetAllAsync_Should_Return_TenNguonVon_Correctly()
        {
            // Arrange
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var nv1 = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_VCSH", Name = "Vốn chủ sở hữu", IsActive = true };
            var nv2 = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_VAY", Name = "Vốn vay thương mại", IsActive = true };
            _dbContext.NguonVons.AddRange(nv1, nv2);
            await _dbContext.SaveChangesAsync();

            // Project 1: Single funding source
            var createDto1 = new CreateDuAnDto
            {
                Code = "DA-NV-001",
                Name = "Dự án Nguồn Vốn Đơn",
                LoaiDuAn = 1,
                DuToanPheDuyet = 500000000m,
                DanhSachNguonVon = new List<CreateDuAnNguonVonDto>
                {
                    new CreateDuAnNguonVonDto { NguonVonId = nv1.Id, SoTien = 500000000m }
                }
            };
            var proj1 = await _duAnService.CreateAsync(createDto1);

            // Project 2: Multiple funding sources
            var createDto2 = new CreateDuAnDto
            {
                Code = "DA-NV-002",
                Name = "Dự án Nguồn Vốn Đa Nguồn",
                LoaiDuAn = 1,
                DuToanPheDuyet = 1000000000m,
                DanhSachNguonVon = new List<CreateDuAnNguonVonDto>
                {
                    new CreateDuAnNguonVonDto { NguonVonId = nv1.Id, SoTien = 600000000m },
                    new CreateDuAnNguonVonDto { NguonVonId = nv2.Id, SoTien = 400000000m }
                }
            };
            var proj2 = await _duAnService.CreateAsync(createDto2);

            // Act - GetById
            var fetched1 = await _duAnService.GetByIdAsync(proj1.Id);
            var fetched2 = await _duAnService.GetByIdAsync(proj2.Id);

            // Assert - GetById
            fetched1.Should().NotBeNull();
            fetched1!.TenNguonVon.Should().Be("Vốn chủ sở hữu");
            fetched1.NguonVonName.Should().Be("Vốn chủ sở hữu");
            fetched1.NguonVonId.Should().Be(nv1.Id);

            fetched1.ThongTinNguonVon.Should().Contain("Vốn chủ sở hữu");
            fetched1.DanhSachNguonVon.Should().HaveCount(1);
            fetched1.DanhSachNguonVon![0].SoTien.Should().Be(500000000m);

            fetched2.Should().NotBeNull();
            fetched2!.TenNguonVon.Should().Be("Vốn chủ sở hữu, Vốn vay thương mại");
            fetched2.NguonVonName.Should().Be("Vốn chủ sở hữu, Vốn vay thương mại");
            fetched2.ThongTinNguonVon.Should().Contain("Vốn chủ sở hữu").And.Contain("Vốn vay thương mại");
            fetched2.DanhSachNguonVon.Should().HaveCount(2);

            // Act - GetAll
            var paged = await _duAnService.GetAllAsync(new DuAnFilterDto { Page = 1, PageSize = 10 });
            var pagedItem1 = paged.Items.FirstOrDefault(x => x.Id == proj1.Id);
            var pagedItem2 = paged.Items.FirstOrDefault(x => x.Id == proj2.Id);

            // Assert - GetAll
            pagedItem1.Should().NotBeNull();
            pagedItem1!.TenNguonVon.Should().Be("Vốn chủ sở hữu");
            pagedItem1.NguonVonName.Should().Be("Vốn chủ sở hữu");
            pagedItem1.DanhSachNguonVon.Should().HaveCount(1);
            pagedItem1.DanhSachNguonVon![0].SoTien.Should().Be(500000000m);

            pagedItem2.Should().NotBeNull();
            pagedItem2!.TenNguonVon.Should().Be("Vốn chủ sở hữu, Vốn vay thương mại");
            pagedItem2.NguonVonName.Should().Be("Vốn chủ sở hữu, Vốn vay thương mại");
            pagedItem2.DanhSachNguonVon.Should().HaveCount(2);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
