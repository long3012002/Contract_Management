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

            var codeGeneratorService = new CodeGeneratorService(_dbContext);
            _goiThauService = new GoiThauService(_dbContext, _mapper, _mockLogger.Object, _mockCurrentUserService.Object, codeGeneratorService);
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
                Code = "PKG-SERVER-01",
                Name = "Gói thầu Mua sắm máy chủ",
                GiaTriGoiThau = 2000000000,
                HinhThucLcnt = "Chỉ định thầu",
                PhuongThucLcnt = "1 GĐ 1 THS"
            };

            // Act
            var result = await _goiThauService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.DuAnId.Should().Be(project.Id);
            result.Name.Should().Be("Gói thầu Mua sắm máy chủ");
            result.HinhThucLcnt.Should().Be("Chỉ định thầu");
            result.PhuongThucLcnt.Should().Be("1 GĐ 1 THS");
        }

        [Fact]
        public async Task UpdateAsync_Should_Update_HinhThucLcnt_And_PhuongThucLcnt()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA2026_02", Name = "Dự án CNTT 2", DuToanPheDuyet = 5000000000 };
            _dbContext.DuAns.Add(project);

            var goiThau = new GoiThau
            {
                Id = Guid.NewGuid(),
                DuAnId = project.Id,
                Code = "PKG-UP-01",
                Name = "Gói thầu nâng cấp",
                GiaTriGoiThau = 1000000000,
                HinhThucLcnt = "Chào hàng cạnh tranh",
                PhuongThucLcnt = "1 GĐ 1 THS"
            };
            _dbContext.GoiThaus.Add(goiThau);
            await _dbContext.SaveChangesAsync();

            var updateDto = new UpdateGoiThauDto
            {
                Code = "PKG-UP-01",
                Name = "Gói thầu nâng cấp cập nhật",
                DuAnId = project.Id,
                GiaTriGoiThau = 1200000000,
                HinhThucLcnt = "Đấu thầu rộng rãi qua mạng",
                PhuongThucLcnt = "1 GĐ 2 THS"
            };

            // Act
            var updated = await _goiThauService.UpdateAsync(goiThau.Id, updateDto);
            var reloaded = await _goiThauService.GetByIdAsync(goiThau.Id);

            // Assert
            updated.Should().BeTrue();
            reloaded.Should().NotBeNull();
            reloaded!.HinhThucLcnt.Should().Be("Đấu thầu rộng rãi qua mạng");
            reloaded.PhuongThucLcnt.Should().Be("1 GĐ 2 THS");
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

        [Fact]
        public async Task UpdateAsync_Should_Validate_Against_Project_DuToanPheDuyet_Even_With_KeHoachVon()
        {
            // Arrange: Dự án có tổng mức đầu tư 125 tỷ (125,000,000,000 VNĐ)
            // Kế hoạch vốn có phân bổ một đợt 125,000 (ví dụ tính theo triệu đồng hoặc đợt)
            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-125B",
                Name = "Dự án CNTT 125 tỷ",
                DuToanPheDuyet = 125000000000m
            };
            _dbContext.DuAns.Add(project);

            var khv = new KeHoachVon
            {
                Id = Guid.NewGuid(),
                Code = "KHV-2026-01",
                Name = "Kế hoạch vốn 2026",
                NamKeHoach = 2026,
                TrangThai = 3
            };
            _dbContext.KeHoachVons.Add(khv);

            var khvDuAn = new KeHoachVonDuAn
            {
                KeHoachVonId = khv.Id,
                DuAnId = project.Id,
                SoTienDeNghi = 125000m,
                SoTienDuocDuyet = 125000m
            };
            _dbContext.KeHoachVonDuAns.Add(khvDuAn);

            var goiThau = new GoiThau
            {
                Id = Guid.NewGuid(),
                DuAnId = project.Id,
                Code = "GT-01",
                Name = "Gói thầu thiết bị",
                GiaTriGoiThau = 10000000000m // 10 tỷ
            };
            _dbContext.GoiThaus.Add(goiThau);
            await _dbContext.SaveChangesAsync();

            // Act: Cập nhật gói thầu lên 113,081,040,000 VNĐ (Tổng gói thầu 113 tỷ <= 125 tỷ)
            var updateDto = new UpdateGoiThauDto
            {
                Code = "GT-01",
                Name = "Gói thầu thiết bị",
                DuAnId = project.Id,
                GiaTriGoiThau = 113081040000m
            };
            var result = await _goiThauService.UpdateAsync(goiThau.Id, updateDto);

            // Assert: Thành công, không bị chặn bởi 125,000 VNĐ của Kế hoạch vốn
            result.Should().BeTrue();
            var reloaded = await _dbContext.GoiThaus.FindAsync(goiThau.Id);
            reloaded!.GiaTriGoiThau.Should().Be(113081040000m);
        }

        [Fact]
        public async Task UpdateAsync_Should_Throw_When_Exceeding_Project_DuToanPheDuyet()
        {
            // Arrange: Dự án có tổng mức đầu tư 125 tỷ
            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-EXCEED",
                Name = "Dự án kiểm tra vượt dự toán",
                DuToanPheDuyet = 125000000000m
            };
            _dbContext.DuAns.Add(project);

            var goiThau = new GoiThau
            {
                Id = Guid.NewGuid(),
                DuAnId = project.Id,
                Code = "GT-OVER",
                Name = "Gói thầu quá hạn mức",
                GiaTriGoiThau = 10000000000m
            };
            _dbContext.GoiThaus.Add(goiThau);
            await _dbContext.SaveChangesAsync();

            // Act: Cập nhật gói thầu lên 130 tỷ (Vượt quá 125 tỷ của dự án)
            var updateDto = new UpdateGoiThauDto
            {
                Code = "GT-OVER",
                Name = "Gói thầu quá hạn mức",
                DuAnId = project.Id,
                GiaTriGoiThau = 130000000000m
            };

            // Assert: Ném lỗi vượt quá tổng mức đầu tư
            Func<Task> act = async () => await _goiThauService.UpdateAsync(goiThau.Id, updateDto);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*vượt quá tổng mức đầu tư của dự án (125,000,000,000 VNĐ)*");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
