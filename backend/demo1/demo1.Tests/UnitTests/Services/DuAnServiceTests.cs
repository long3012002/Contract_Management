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

        [Fact]
        public async Task CreateAsync_Should_Throw_When_SourceProjectIds_Contains_Duplicates()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var sp = new DuAn { Id = Guid.NewGuid(), Code = "SP-001", Name = "Nguồn 1", LoaiDuAn = 1, DuToanPheDuyet = 1000000m, DaTrienKhai = false };
            _dbContext.DuAns.Add(sp);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "TK-001",
                Name = "Triển khai 1",
                LoaiDuAn = 2,
                SourceProjectIds = new List<Guid> { sp.Id, sp.Id }
            };

            Func<Task> act = async () => await _duAnService.CreateAsync(createDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*chứa mã dự án trùng lặp*");
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_SourceProject_Already_Linked_In_DuAnNguonTrienKhai()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var sp = new DuAn { Id = Guid.NewGuid(), Code = "SP-LINKED", Name = "Dự án nguồn đã liên kết", LoaiDuAn = 1, DuToanPheDuyet = 5000000m, DaTrienKhai = false };
            var tkExisting = new DuAn { Id = Guid.NewGuid(), Code = "TK-EXIST", Name = "Triển khai cũ", LoaiDuAn = 2, DuToanPheDuyet = 5000000m, DaTrienKhai = true };
            var link = new DuAnNguonTrienKhai { TrienKhaiProjectId = tkExisting.Id, NguonProjectId = sp.Id.ToString() };

            _dbContext.DuAns.AddRange(sp, tkExisting);
            _dbContext.DuAnNguonTrienKhais.Add(link);
            await _dbContext.SaveChangesAsync();

            var newTkDto = new CreateDuAnDto
            {
                Code = "TK-NEW",
                Name = "Triển khai mới",
                LoaiDuAn = 2,
                SourceProjectIds = new List<Guid> { sp.Id }
            };

            Func<Task> act = async () => await _duAnService.CreateAsync(newTkDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã thuộc về một dự án triển khai khác*");
        }

        [Fact]
        public async Task CreateAsync_Should_Throw_When_LoaiDuAn_Is_1_And_SourceProjectIds_Provided()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateDuAnDto
            {
                Code = "SP-INVALID",
                Name = "Dự án nguồn có liên kết",
                LoaiDuAn = 1,
                SourceProjectIds = new List<Guid> { Guid.NewGuid() }
            };

            Func<Task> act = async () => await _duAnService.CreateAsync(createDto);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*không thể liên kết đến dự án nguồn khác*");
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_SourceProject_Already_Linked_To_Another_Project()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var sp1 = new DuAn { Id = Guid.NewGuid(), Code = "SP-01", Name = "Nguồn 1", LoaiDuAn = 1, DuToanPheDuyet = 1000000m, DaTrienKhai = true };
            var sp2 = new DuAn { Id = Guid.NewGuid(), Code = "SP-02", Name = "Nguồn 2", LoaiDuAn = 1, DuToanPheDuyet = 2000000m, DaTrienKhai = true };
            var tk1 = new DuAn { Id = Guid.NewGuid(), Code = "TK-01", Name = "Triển khai 1", LoaiDuAn = 2, DuToanPheDuyet = 1000000m, DaTrienKhai = true };
            var tk2 = new DuAn { Id = Guid.NewGuid(), Code = "TK-02", Name = "Triển khai 2", LoaiDuAn = 2, DuToanPheDuyet = 2000000m, DaTrienKhai = true };

            var link1 = new DuAnNguonTrienKhai { TrienKhaiProjectId = tk1.Id, NguonProjectId = sp1.Id.ToString() };
            var link2 = new DuAnNguonTrienKhai { TrienKhaiProjectId = tk2.Id, NguonProjectId = sp2.Id.ToString() };

            _dbContext.DuAns.AddRange(sp1, sp2, tk1, tk2);
            _dbContext.DuAnNguonTrienKhais.AddRange(link1, link2);
            await _dbContext.SaveChangesAsync();

            // Attempt to update tk2 to also link sp1 (which belongs to tk1)
            var updateDto = new UpdateDuAnDto
            {
                Code = tk2.Code,
                Name = tk2.Name,
                SourceProjectIds = new List<Guid> { sp1.Id }
            };

            Func<Task> act = async () => await _duAnService.UpdateAsync(tk2.Id, updateDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*đã thuộc về một dự án triển khai khác*");
        }

        [Fact]
        public async Task UpdateAsync_Should_Succeed_When_Keeping_Own_SourceProjects()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var sp = new DuAn { Id = Guid.NewGuid(), Code = "SP-OWN", Name = "Nguồn sở hữu", LoaiDuAn = 1, DuToanPheDuyet = 1000000m, DaTrienKhai = true };
            var tk = new DuAn { Id = Guid.NewGuid(), Code = "TK-OWN", Name = "Triển khai sở hữu", LoaiDuAn = 2, DuToanPheDuyet = 1000000m, DaTrienKhai = true };
            var link = new DuAnNguonTrienKhai { TrienKhaiProjectId = tk.Id, NguonProjectId = sp.Id.ToString() };

            _dbContext.DuAns.AddRange(sp, tk);
            _dbContext.DuAnNguonTrienKhais.Add(link);
            await _dbContext.SaveChangesAsync();

            var updateDto = new UpdateDuAnDto
            {
                Code = tk.Code,
                Name = "Triển khai đã đổi tên",
                SourceProjectIds = new List<Guid> { sp.Id }
            };

            var result = await _duAnService.UpdateAsync(tk.Id, updateDto);
            result.Should().BeTrue();

            var reloaded = await _dbContext.DuAns.FindAsync(tk.Id);
            reloaded!.Name.Should().Be("Triển khai đã đổi tên");
        }

        [Fact]
        public async Task CreateRangeAsync_Should_Throw_When_Batch_Contains_Duplicate_SourceProjects()
        {
            var user = new User { Username = "test_admin", FullName = "Admin Test", IsActive = true, IsSystemAdmin = true };
            _dbContext.Users.Add(user);

            var sp = new DuAn { Id = Guid.NewGuid(), Code = "SP-SHARED", Name = "Nguồn dùng chung", LoaiDuAn = 1, DuToanPheDuyet = 1000000m, DaTrienKhai = false };
            _dbContext.DuAns.Add(sp);
            await _dbContext.SaveChangesAsync();

            var dtos = new List<CreateDuAnDto>
            {
                new CreateDuAnDto { Code = "TK-B1", Name = "TK Batch 1", LoaiDuAn = 2, SourceProjectIds = new List<Guid> { sp.Id } },
                new CreateDuAnDto { Code = "TK-B2", Name = "TK Batch 2", LoaiDuAn = 2, SourceProjectIds = new List<Guid> { sp.Id } }
            };

            Func<Task> act = async () => await _duAnService.CreateRangeAsync(dtos);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*được liên kết nhiều hơn một lần trong danh sách tạo*");
        }

        [Fact]
        public async Task AdjustBudgetAsync_Should_Throw_KeyNotFoundException_When_Project_Not_Found()
        {
            var nonExistentId = Guid.NewGuid();
            var dto = new CreateDieuChinhDuAnDto { GiaTriDieuChinh = 100000, LyDoDieuChinh = "Tăng ngân sách" };

            Func<Task> act = async () => await _duAnService.AdjustBudgetAsync(nonExistentId, dto);
            await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("Không tìm thấy dự án.");
        }

        [Fact]
        public async Task AdjustBudgetAsync_Should_Throw_UnauthorizedAccessException_When_User_Has_No_Permission()
        {
            var owner = new User { Id = Guid.NewGuid(), Username = "project_owner", FullName = "Owner", IsActive = true, IsSystemAdmin = false };
            var unauthorizedUser = new User { Id = Guid.NewGuid(), Username = "unauthorized_user", FullName = "No Perm", IsActive = true, IsSystemAdmin = false };
            _dbContext.Users.AddRange(owner, unauthorizedUser);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-PERM-01",
                Name = "Dự án bảo mật",
                LoaiDuAn = 1,
                DuToanPheDuyet = 5000000m,
                CreatedByUserId = owner.Id,
                ChuDuAnId = owner.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("unauthorized_user");

            var dto = new CreateDieuChinhDuAnDto { GiaTriDieuChinh = 100000, LyDoDieuChinh = "Tăng ngân sách" };

            Func<Task> act = async () => await _duAnService.AdjustBudgetAsync(project.Id, dto);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền thực hiện thao tác trên dự án này.");
        }

        [Fact]
        public async Task AdvanceStatusAsync_Should_Throw_KeyNotFoundException_When_Project_Not_Found()
        {
            var nonExistentId = Guid.NewGuid();

            Func<Task> act = async () => await _duAnService.AdvanceStatusAsync(nonExistentId);
            await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("Không tìm thấy dự án.");
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
                LoaiDuAn = 1,
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
                .WithMessage("Không tìm thấy dự án.");
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
                LoaiDuAn = 1,
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

        [Fact]
        public async Task GetAdjustmentsAsync_Should_Throw_KeyNotFoundException_When_Project_Not_Found()
        {
            var nonExistentId = Guid.NewGuid();

            Func<Task> act = async () => await _duAnService.GetAdjustmentsAsync(nonExistentId);
            await act.Should().ThrowAsync<System.Collections.Generic.KeyNotFoundException>()
                .WithMessage("Không tìm thấy dự án.");
        }

        [Fact]
        public async Task GetAdjustmentsAsync_Should_Throw_UnauthorizedAccessException_When_User_Has_No_Permission()
        {
            var owner = new User { Id = Guid.NewGuid(), Username = "owner_4", FullName = "Owner 4", IsActive = true, IsSystemAdmin = false };
            var unauthorizedUser = new User { Id = Guid.NewGuid(), Username = "unauthorized_4", FullName = "No Perm 4", IsActive = true, IsSystemAdmin = false };
            _dbContext.Users.AddRange(owner, unauthorizedUser);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-PERM-04",
                Name = "Dự án tra cứu điều chỉnh",
                LoaiDuAn = 1,
                CreatedByUserId = owner.Id,
                ChuDuAnId = owner.Id
            };
            _dbContext.DuAns.Add(project);
            await _dbContext.SaveChangesAsync();

            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("unauthorized_4");

            Func<Task> act = async () => await _duAnService.GetAdjustmentsAsync(project.Id);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Bạn không có quyền thực hiện thao tác trên dự án này.");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
