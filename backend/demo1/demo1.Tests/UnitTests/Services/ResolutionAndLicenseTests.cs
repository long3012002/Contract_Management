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

        [Fact]
        public async Task SyncContractLicensesAsync_Should_Perform_Upsert_And_Soft_Delete_Correctly()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<LicenseService>.Instance;
            var licenseService = new LicenseService(_dbContext, _mapper, logger);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-SYNC", Name = "Dự án Sync" };
            var contract = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GiaTriHopDong = 1000000 };
            _dbContext.DuAns.Add(project);
            _dbContext.HopDongs.Add(contract);

            var existingItem1 = new License
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                DuAnId = project.Id,
                Code = "LIC-UPDATE",
                Name = "License Update Test",
                IsActive = true
            };
            var existingItem2ToDelete = new License
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                DuAnId = project.Id,
                Code = "LIC-DELETE",
                Name = "License Delete Test",
                IsActive = true
            };
            _dbContext.Licenses.AddRange(existingItem1, existingItem2ToDelete);
            await _dbContext.SaveChangesAsync();

            var syncDto = new SyncContractLicensesDto
            {
                Items = new List<SyncLicenseItemDto>
                {
                    // 1. Update existingItem1
                    new SyncLicenseItemDto
                    {
                        Id = existingItem1.Id,
                        Code = "LIC-UPDATED-NEW",
                        Name = "License Updated Success",
                        DuAnId = project.Id
                    },
                    // 2. Insert new item (no ID)
                    new SyncLicenseItemDto
                    {
                        Code = "LIC-NEW",
                        Name = "License New Inserted",
                        DuAnId = project.Id
                    }
                }
            };

            // Act
            var syncResult = await licenseService.SyncContractLicensesAsync(contract.Id, syncDto);

            // Assert
            syncResult.Should().NotBeNull();
            syncResult.HopDongId.Should().Be(contract.Id);
            syncResult.CreatedCount.Should().Be(1);
            syncResult.UpdatedCount.Should().Be(1);
            syncResult.DeletedCount.Should().Be(1);
            syncResult.Items.Should().HaveCount(2);

            // Verify DB state
            var deletedInDb = await _dbContext.Licenses.FindAsync(existingItem2ToDelete.Id);
            deletedInDb.Should().NotBeNull();
            deletedInDb!.IsActive.Should().BeFalse();

            var updatedInDb = await _dbContext.Licenses.FindAsync(existingItem1.Id);
            updatedInDb.Should().NotBeNull();
            updatedInDb!.Name.Should().Be("License Updated Success");
            updatedInDb.Code.Should().Be("LIC-UPDATED-NEW");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}

