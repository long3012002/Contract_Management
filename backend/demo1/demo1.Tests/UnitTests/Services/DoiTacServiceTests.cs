using System;
using System.Threading.Tasks;
using AutoMapper;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class DoiTacServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly DoiTacService _doiTacService;

        public DoiTacServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _doiTacService = new DoiTacService(_dbContext, _mapper);
        }

        [Fact]
        public async Task TC55_CreateAsync_Should_Create_DoiTac_Successfully()
        {
            // Arrange
            var createDto = new CreateDoiTacDto
            {
                Code = "DT001",
                Name = "Công ty TNHH Giải pháp CNTT Coop",
                TaxCode = "0101234567",
                Address = "Hà Nội",
                Phone = "02431234567",
                Email = "contact@coopsolutions.vn"
            };

            // Act
            var result = await _doiTacService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Công ty TNHH Giải pháp CNTT Coop");
            result.TaxCode.Should().Be("0101234567");
        }

        [Fact]
        public async Task TC56_Check_Duplicate_MaSoThue_Should_Return_True()
        {
            // Arrange
            var existing = new DoiTac { Id = Guid.NewGuid(), Code = "DT-EXISTING", Name = "Đối tác cũ", TaxCode = "0101234567" };
            _dbContext.DoiTacs.Add(existing);
            await _dbContext.SaveChangesAsync();

            // Act
            var isDuplicateMST = await _dbContext.DoiTacs.AnyAsync(d => d.TaxCode == "0101234567");

            // Assert
            isDuplicateMST.Should().BeTrue();
        }

        [Fact]
        public async Task TC57_GetByIdAsync_Should_Calculate_ContractCount_Correctly()
        {
            // Arrange
            var doiTac = new DoiTac { Id = Guid.NewGuid(), Code = "DT-HIST", Name = "Đối tác lịch sử thầu" };
            _dbContext.DoiTacs.Add(doiTac);

            var hd1 = new HopDong { Id = Guid.NewGuid(), Code = "HD1", Name = "Hợp đồng 1", NhaThauId = doiTac.Id };
            var hd2 = new HopDong { Id = Guid.NewGuid(), Code = "HD2", Name = "Hợp đồng 2", NhaThauId = doiTac.Id };
            _dbContext.HopDongs.AddRange(hd1, hd2);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _doiTacService.GetByIdAsync(doiTac.Id);

            // Assert
            result.Should().NotBeNull();
            result!.ContractCount.Should().Be(2);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
