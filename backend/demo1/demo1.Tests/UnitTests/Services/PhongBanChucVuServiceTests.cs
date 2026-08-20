using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class PhongBanChucVuServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly PhongBanService _phongBanService;
        private readonly ChucVuService _chucVuService;

        public PhongBanChucVuServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMemoryCache();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();

            _mapper = serviceProvider.GetRequiredService<IMapper>();
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();

            _phongBanService = new PhongBanService(_dbContext, _mapper, _cache);
            _chucVuService = new ChucVuService(_dbContext, _mapper, _cache);
        }

        [Fact]
        public async Task TC19_CreateAsync_Should_Create_PhongBan_Successfully()
        {
            // Arrange
            var createDto = new CreatePhongBanDto
            {
                TenPhongBan = "PGD Cầu Giấy"
            };

            // Act
            var result = await _phongBanService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.TenPhongBan.Should().Be("PGD Cầu Giấy");

            var dbItem = await _dbContext.PhongBans.FindAsync(result.Id);
            dbItem.Should().NotBeNull();
            dbItem!.TenPhongBan.Should().Be("PGD Cầu Giấy");
        }

        [Fact]
        public async Task TC20_CreateRangeAsync_Should_Create_Multiple_PhongBans()
        {
            // Arrange
            var dtos = new List<CreatePhongBanDto>
            {
                new CreatePhongBanDto { TenPhongBan = "PGD Ba Đình" },
                new CreatePhongBanDto { TenPhongBan = "PGD Đống Đa" },
                new CreatePhongBanDto { TenPhongBan = "PGD Hoàn Kiếm" }
            };

            // Act
            var result = await _phongBanService.CreateRangeAsync(dtos);

            // Assert
            result.Should().HaveCount(3);
            _dbContext.PhongBans.Count().Should().Be(3);
        }

        [Fact]
        public async Task TC21_ChucVu_Create_And_Update_Should_Work_Correctly()
        {
            // Arrange
            var createDto = new CreateChucVuDto { TenChucVu = "Trưởng phòng CNTT" };

            // Act: Create
            var created = await _chucVuService.CreateAsync(createDto);
            created.Should().NotBeNull();
            created.TenChucVu.Should().Be("Trưởng phòng CNTT");

            // Act: Update
            var updateDto = new UpdateChucVuDto { TenChucVu = "Trưởng phòng Trung tâm CNTT" };
            var updatedResult = await _chucVuService.UpdateAsync(created.Id, updateDto);

            // Assert
            updatedResult.Should().BeTrue();
            var dbItem = await _dbContext.ChucVus.FindAsync(created.Id);
            dbItem!.TenChucVu.Should().Be("Trưởng phòng Trung tâm CNTT");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
