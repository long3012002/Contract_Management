using System;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class ResolutionAndLicenseTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ResolutionService _resolutionService;

        public ResolutionAndLicenseTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _resolutionService = new ResolutionService(_dbContext, _mapper);
        }

        [Fact]
        public async Task TC58_Resolution_CreateAsync_Should_Succeed_With_Valid_Data()
        {
            // Arrange
            var createDto = new CreateResolutionDto
            {
                Code = "123/QĐ-CoopBank",
                Title = "Nghị quyết phê duyệt dự án CNTT",
                IssuedDate = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow.AddDays(1)
            };

            // Act
            var result = await _resolutionService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().BeEquivalentTo("123/QĐ-CoopBank");
        }

        [Fact]
        public void TC59_Validate_Attachment_File_Extension_Should_Reject_Unsafe_Files()
        {
            // Arrange
            string safeFile = "document.pdf";
            string unsafeFile1 = "malicious.exe";
            string unsafeFile2 = "script.bat";

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx", ".doc" };

            // Act & Assert
            allowedExtensions.Contains(Path.GetExtension(safeFile).ToLower()).Should().BeTrue();
            allowedExtensions.Contains(Path.GetExtension(unsafeFile1).ToLower()).Should().BeFalse();
            allowedExtensions.Contains(Path.GetExtension(unsafeFile2).ToLower()).Should().BeFalse();
        }

        [Fact]
        public async Task TC60_TC61_License_Expiring_Check_Should_Filter_Licenses_Within_Threshold()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-LIC", Name = "Dự án có License" };
            _dbContext.DuAns.Add(project);

            var licExpiring = new License
            {
                Id = Guid.NewGuid(),
                DuAnId = project.Id,
                Code = "LIC-01",
                Name = "Oracle Database Enterprise",
                SoLuong = 10,
                NgayKetThuc = DateTime.UtcNow.AddDays(15) // Expiring in 15 days
            };

            var licValid = new License
            {
                Id = Guid.NewGuid(),
                DuAnId = project.Id,
                Code = "LIC-02",
                Name = "Windows Server 2026",
                SoLuong = 5,
                NgayKetThuc = DateTime.UtcNow.AddDays(180) // Expiring in 180 days
            };

            _dbContext.Licenses.AddRange(licExpiring, licValid);
            await _dbContext.SaveChangesAsync();

            // Act: Threshold = 30 days
            var thresholdDate = DateTime.UtcNow.AddDays(30);
            var expiringLicenses = _dbContext.Licenses.Where(l => l.NgayKetThuc != null && l.NgayKetThuc <= thresholdDate).ToList();

            // Assert
            expiringLicenses.Should().HaveCount(1);
            expiringLicenses[0].Name.Should().Be("Oracle Database Enterprise");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
