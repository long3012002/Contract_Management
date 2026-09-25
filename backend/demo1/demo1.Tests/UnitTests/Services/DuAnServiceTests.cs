using System;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Services.Implements.SubServices;
using demo1.Services.Interfaces;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using demo1.Hubs;
using Microsoft.EntityFrameworkCore;
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

            var securityService = new DuAnSecurityService(_dbContext, _mockCurrentUserService.Object);
            var nguonLinkService = new DuAnNguonLinkService(_dbContext, _mapper, securityService);
            var budgetService = new DuAnBudgetService(_dbContext, _mapper, securityService);
            var cascadeService = new DuAnCascadeService(_dbContext, _mockCurrentUserService.Object, nguonLinkService);
            var auditService = new DuAnAuditService(_dbContext, securityService);
            var notificationService = new DuAnNotificationService(_dbContext, _mockCurrentUserService.Object, _mockHubContext.Object);

            var codeGeneratorService = new CodeGeneratorService(_dbContext);
            _duAnService = new DuAnService(_dbContext, _mapper, _mockCurrentUserService.Object, securityService, nguonLinkService, budgetService, cascadeService, auditService, notificationService, codeGeneratorService);
        }

        [Fact]
        public async Task CreateAsync_Should_Save_Project_Successfully_When_Valid()
        {
            var createDto = new CreateDuAnDto
            {
                Code = "PRJ_SRC-DA-TEST-001",
                Name = "Dự án Thử nghiệm tự động",
                Description = "Mô tả dự án kiểm thử"
            };

            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var result = await _duAnService.CreateAsync(createDto);

            result.Should().NotBeNull();
            result.Code.Should().Be("PRJ_SRC-DA-TEST-001");
            result.Name.Should().Be("Dự án Thử nghiệm tự động");

            var dbProject = await _dbContext.DuAns.FindAsync(result.Id);
            dbProject.Should().NotBeNull();
            dbProject!.Code.Should().Be("PRJ_SRC-DA-TEST-001");
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
                Code = "PRJ_SRC-DA-PHANKY-001",
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
                Code = "PRJ_SRC-DA-PHANKY-002",
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
                Code = "PRJ_SRC-DA-PHANKY-002",
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
                Code = "PRJ_SRC-DA-NV-001",
                Name = "Dự án Nguồn Vốn Đơn",
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
                Code = "PRJ_SRC-DA-NV-002",
                Name = "Dự án Nguồn Vốn Đa Nguồn",
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

        [Fact]
        public async Task AdvanceStatusAsync_Should_Throw_KeyNotFoundException_When_Project_Not_Found()
        {
            var nonExistentId = Guid.NewGuid();

            Func<Task> act = async () => await _duAnService.AdvanceStatusAsync(nonExistentId);
            await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("*Không tìm thấy*");
        }

        [Fact]
        public async Task AdvanceStatusAsync_Should_Throw_UnauthorizedAccessException_When_User_Has_No_Permission()
        {
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_2", FullName = "Owner 2", IsActive = true, IsSystemAdmin = false };
            var unauthorizedUser = new User { Id = Guid.NewGuid(), Username = "unauthorized_2", FullName = "No Perm 2", IsActive = true, IsSystemAdmin = false };
            _dbContext.Users.AddRange(owner, unauthorizedUser);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-PERM-02",
                Name = "Dự án chuyển trạng thái",
                TrangThai = 1,
                CreatedByUserId = owner.Id,
                ChuDuAnId = owner.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("unauthorized_2");

            Func<Task> act = async () => await _duAnService.AdvanceStatusAsync(project.Id);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền thực hiện thao tác trên dự án này.");
        }

        [Fact]
        public async Task CloseProjectAsync_Should_Throw_KeyNotFoundException_When_Project_Not_Found()
        {
            var nonExistentId = Guid.NewGuid();

            Func<Task> act = async () => await _duAnService.CloseProjectAsync(nonExistentId);
            await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("*Không tìm thấy*");
        }

        [Fact]
        public async Task CloseProjectAsync_Should_Throw_UnauthorizedAccessException_When_User_Has_No_Permission()
        {
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_3", FullName = "Owner 3", IsActive = true, IsSystemAdmin = false };
            var unauthorizedUser = new User { Id = Guid.NewGuid(), Username = "unauthorized_3", FullName = "No Perm 3", IsActive = true, IsSystemAdmin = false };
            _dbContext.Users.AddRange(owner, unauthorizedUser);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-PERM-03",
                Name = "Dự án đóng",
                TrangThai = 1,
                CreatedByUserId = owner.Id,
                ChuDuAnId = owner.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("unauthorized_3");

            Func<Task> act = async () => await _duAnService.CloseProjectAsync(project.Id);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền thực hiện thao tác trên dự án này.");
        }

        [Theory]
        [InlineData((int)TrangThaiDuAn.Draft)]
        [InlineData((int)TrangThaiDuAn.Approved)]
        [InlineData((int)TrangThaiDuAn.Implementing)]
        [InlineData((int)TrangThaiDuAn.Completed)]
        [InlineData((int)TrangThaiDuAn.Merged)]
        public async Task DeleteAsync_Should_Delete_Project_Regardless_Of_TrangThai(int trangThai)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "test_admin", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = $"DA-STATUS-{trangThai}",
                Name = $"Dự án trạng thái {trangThai}",
                TrangThai = trangThai,
                CreatedByUserId = user.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            var result = await _duAnService.DeleteAsync(project.Id);

            result.Should().BeTrue();
            var deletedProject = await _dbContext.DuAns.FindAsync(project.Id);
            deletedProject.Should().BeNull();
        }

        [Theory]
        [InlineData((int)TrangThaiDuAn.Draft)]
        [InlineData((int)TrangThaiDuAn.Approved)]
        [InlineData((int)TrangThaiDuAn.Implementing)]
        [InlineData((int)TrangThaiDuAn.Completed)]
        [InlineData((int)TrangThaiDuAn.Merged)]
        public async Task SoftDeleteAsync_Should_SoftDelete_Project_Regardless_Of_TrangThai(int trangThai)
        {
            var user = new User { Id = Guid.NewGuid(), Username = "test_admin", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = $"DA-SOFT-{trangThai}",
                Name = $"Dự án xóa mềm trạng thái {trangThai}",
                TrangThai = trangThai,
                CreatedByUserId = user.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            var result = await _duAnService.SoftDeleteAsync(project.Id);

            result.Should().BeTrue();
            var softDeletedProject = await _dbContext.DuAns.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == project.Id);
            softDeletedProject.Should().NotBeNull();
            softDeletedProject!.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_Should_Assign_KeHoachVon_When_FundingSource_Year_Matches()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var nv = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_TEST", Name = "Nguồn vốn Test", IsActive = true };
            _dbContext.NguonVons.Add(nv);

            var khv2026 = new KeHoachVon { Id = Guid.NewGuid(), NamKeHoach = 2026, LoaiKeHoach = 2, Code = "KHV-2026", Name = "Kế hoạch vốn 2026" };
            _dbContext.KeHoachVons.Add(khv2026);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "PRJ_SRC-DA-KHV-001",
                Name = "Dự án gán KHV hợp lệ",
                DuToanPheDuyet = 500000000m,
                DanhSachNguonVon = new List<CreateDuAnNguonVonDto>
                {
                    new CreateDuAnNguonVonDto { NguonVonId = nv.Id, Nam = 2026, SoTien = 500000000m }
                },
                KeHoachVonIds = new List<Guid> { khv2026.Id }
            };

            var result = await _duAnService.CreateAsync(createDto);

            result.Should().NotBeNull();
            result.DanhSachKeHoachVon.Should().HaveCount(1);
            result.DanhSachKeHoachVon[0].KeHoachVonId.Should().Be(khv2026.Id);
            result.DanhSachKeHoachVon[0].NamKeHoach.Should().Be(2026);
            result.DanhSachKeHoachVon[0].SoTienDeNghi.Should().Be(500000000m);
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_No_FundingSource_Year_Matches_KeHoachVon_Year()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var nv = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_TEST2", Name = "Nguồn vốn Test 2", IsActive = true };
            _dbContext.NguonVons.Add(nv);

            var khv2027 = new KeHoachVon { Id = Guid.NewGuid(), NamKeHoach = 2027, LoaiKeHoach = 2, Code = "KHV-2027", Name = "Kế hoạch vốn 2027" };
            _dbContext.KeHoachVons.Add(khv2027);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "PRJ_SRC-DA-KHV-002",
                Name = "Dự án gán KHV không hợp lệ năm",
                DuToanPheDuyet = 500000000m,
                DanhSachNguonVon = new List<CreateDuAnNguonVonDto>
                {
                    new CreateDuAnNguonVonDto { NguonVonId = nv.Id, Nam = 2026, SoTien = 500000000m }
                },
                KeHoachVonIds = new List<Guid> { khv2027.Id }
            };

            Func<Task> act = async () => await _duAnService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*không có nguồn vốn nào thuộc năm 2027*");
        }

        [Fact]
        public async Task UpdateAsync_Should_Sync_KeHoachVon_When_FundingSource_Year_Matches()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var nv = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_TEST3", Name = "Nguồn vốn Test 3", IsActive = true };
            _dbContext.NguonVons.Add(nv);

            var khv2026 = new KeHoachVon { Id = Guid.NewGuid(), NamKeHoach = 2026, LoaiKeHoach = 2, Code = "KHV-2026-B", Name = "Kế hoạch vốn 2026 B" };
            _dbContext.KeHoachVons.Add(khv2026);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "PRJ_SRC-DA-KHV-003",
                Name = "Dự án update KHV",
                DuToanPheDuyet = 800000000m,
                DanhSachNguonVon = new List<CreateDuAnNguonVonDto>
                {
                    new CreateDuAnNguonVonDto { NguonVonId = nv.Id, Nam = 2026, SoTien = 800000000m }
                }
            };
            var created = await _duAnService.CreateAsync(createDto);

            var updateDto = new UpdateDuAnDto
            {
                Code = "PRJ_SRC-DA-KHV-003",
                Name = "Dự án update KHV (đã cập nhật)",
                DuToanPheDuyet = 800000000m,
                DanhSachKeHoachVon = new List<CreateDuAnKeHoachVonDto>
                {
                    new CreateDuAnKeHoachVonDto
                    {
                        KeHoachVonId = khv2026.Id,
                        SoTienDeNghi = 400000000m,
                        SoTienDuocDuyet = 400000000m,
                        GhiChu = "Đã gán thành công"
                    }
                }
            };

            var updateResult = await _duAnService.UpdateAsync(created.Id, updateDto);
            updateResult.Should().BeTrue();

            var updatedProject = await _duAnService.GetByIdAsync(created.Id);
            updatedProject.Should().NotBeNull();
            updatedProject!.DanhSachKeHoachVon.Should().HaveCount(1);
            updatedProject.DanhSachKeHoachVon[0].KeHoachVonId.Should().Be(khv2026.Id);
            updatedProject.DanhSachKeHoachVon[0].SoTienDeNghi.Should().Be(400000000m);
            updatedProject.DanhSachKeHoachVon[0].GhiChu.Should().Be("Đã gán thành công");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}

