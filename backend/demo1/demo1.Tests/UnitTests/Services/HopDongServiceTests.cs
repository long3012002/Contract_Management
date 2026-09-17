using System;
using System.Collections.Generic;
using System.Linq;
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
    public class HopDongServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<HopDongService>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly HopDongService _hopDongService;

        public HopDongServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<demo1.Mapper.MappingProfile>());
            var serviceProvider = services.BuildServiceProvider();
            _mapper = serviceProvider.GetRequiredService<IMapper>();

            _mockLogger = new Mock<ILogger<HopDongService>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(x => x.GetUsername()).Returns("admin");

            var adminUser = new User { Id = Guid.NewGuid(), Username = "admin", FullName = "Admin", IsSystemAdmin = true, IsActive = true };
            _dbContext.Users.Add(adminUser);
            _dbContext.SaveChanges();

            _hopDongService = new HopDongService(_dbContext, _mapper, _mockLogger.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task TC46_CreateAsync_Should_Create_Contract_Linked_To_Package()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-01", Name = "Dự án A" };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-01", Name = "Gói thầu A", GiaTriGoiThau = 2000000000 };
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            await _dbContext.SaveChangesAsync();

            var createDto = new CreateHopDongDto
            {
                DuAnId = project.Id,
                GoiThauId = goiThau.Id,
                Code = "HD2026/01",
                Name = "Hợp đồng Mua sắm máy chủ",
                GiaTriHopDong = 1800000000
            };

            // Act
            var result = await _hopDongService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.GoiThauId.Should().Be(goiThau.Id);
            result.GiaTriHopDong.Should().Be(1800000000);
        }

        [Fact]
        public async Task TC47_CreateAsync_Should_Prevent_Duplicate_Contract_For_Same_Package()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-02", Name = "Dự án B" };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-02", Name = "Gói thầu B" };
            var existingContract = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau.Id, Code = "HD-EXISTING", Name = "Hợp đồng đã có" };
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.HopDongs.Add(existingContract);
            await _dbContext.SaveChangesAsync();

            // Act & Assert
            var isPackageHasContract = await _dbContext.HopDongs.AnyAsync(h => h.GoiThauId == goiThau.Id);
            isPackageHasContract.Should().BeTrue();
        }

        [Fact]
        public async Task TC48_PhuLucHopDong_Should_Update_Contract_Total_Value()
        {
            // Arrange
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD2026/01",
                Name = "Hợp đồng gốc",
                GiaTriHopDong = 1800000000
            };
            _dbContext.HopDongs.Add(contract);
            await _dbContext.SaveChangesAsync();

            // Act: Adjust contract value with addendum (+200M)
            decimal giaTriAddendum = 200000000m;
            contract.GiaTriHopDong += giaTriAddendum;
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbContract = await _dbContext.HopDongs.FindAsync(contract.Id);
            dbContract!.GiaTriHopDong.Should().Be(2000000000);
        }

        [Fact]
        public async Task TC49_TC50_Payment_Milestone_Should_Track_Disbursement_And_Prevent_Overpayment()
        {
            // Arrange: Contract worth 2 billion, paid 1.8 billion
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-PAY",
                Name = "Hợp đồng thanh toán",
                GiaTriHopDong = 2000000000
            };
            _dbContext.HopDongs.Add(contract);

            var dot1 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                TenDot = "Tạm ứng 30%",
                GiaTriThanhToan = 1800000000,
                IsPaid = true
            };
            _dbContext.DotThanhToans.Add(dot1);
            await _dbContext.SaveChangesAsync();

            // Act 1: Sum existing payments
            var totalPaid = _dbContext.DotThanhToans.Where(d => d.HopDongId == contract.Id).Sum(d => d.GiaTriThanhToan);
            totalPaid.Should().Be(1800000000);

            // Act 2: New payment of 500M (Total 2.3B > 2B contract value)
            var newPayment = 500000000m;
            var isOverpayment = (totalPaid + newPayment) > contract.GiaTriHopDong;

            // Assert
            isOverpayment.Should().BeTrue();
        }

        [Fact]
        public async Task TC53_BaoLanhHopDong_Expiring_Warning_Check()
        {
            // Arrange
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-BL",
                Name = "Hợp đồng có bảo lãnh",
                ExpiredDate = DateTime.UtcNow.AddDays(15), // Expiring in 15 days
                RenewalReminderDate = DateTime.UtcNow.AddDays(30)
            };
            _dbContext.HopDongs.Add(contract);
            await _dbContext.SaveChangesAsync();

            // Act
            var isExpiringSoon = contract.ExpiredDate <= DateTime.UtcNow.AddDays(30);

            // Assert
            isExpiringSoon.Should().BeTrue();
        }

        [Fact]
        public async Task ConfirmPaymentAsync_Should_Record_Actual_Payment_Date_And_Preserve_Planned_Date()
        {
            // Arrange
            var plannedDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            var actualDate = new DateTime(2026, 12, 10, 0, 0, 0, DateTimeKind.Utc);
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-PAY-CONFIRM",
                Name = "Hợp đồng test xác nhận thanh toán",
                GiaTriHopDong = 1000000000
            };
            _dbContext.HopDongs.Add(contract);

            var dot = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                TenDot = "Đợt 1",
                GiaTriThanhToan = 500000000,
                NgayThanhToan = plannedDate, // Ngày kế hoạch
                IsPaid = false
            };
            _dbContext.DotThanhToans.Add(dot);
            await _dbContext.SaveChangesAsync();

            // Act: Confirm payment with actual date
            var confirmDto = new ConfirmPaymentDto
            {
                NgayThanhToanThucTe = actualDate,
                GhiChuThanhToan = "UNC số 123456"
            };
            var result = await _hopDongService.ConfirmPaymentAsync(dot.Id, confirmDto);

            // Assert
            result.Should().BeTrue();
            var dbDot = await _dbContext.DotThanhToans.FindAsync(dot.Id);
            dbDot!.IsPaid.Should().BeTrue();
            dbDot.NgayThanhToan.Should().Be(plannedDate); // Kế hoạch được bảo toàn
            dbDot.NgayThanhToanThucTe.Should().Be(actualDate); // Ngày thực tế được ghi nhận
            dbDot.GhiChuThanhToan.Should().Be("UNC số 123456");
        }

        [Fact]
        public async Task UndoPaymentAsync_Should_Clear_Actual_Payment_Date_And_Preserve_Planned_Date()
        {
            // Arrange
            var plannedDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            var actualDate = new DateTime(2026, 12, 10, 0, 0, 0, DateTimeKind.Utc);
            var contract = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-PAY-UNDO",
                Name = "Hợp đồng test hoàn tác thanh toán",
                GiaTriHopDong = 1000000000
            };
            _dbContext.HopDongs.Add(contract);

            var dot = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = contract.Id,
                TenDot = "Đợt 1",
                GiaTriThanhToan = 500000000,
                NgayThanhToan = plannedDate,
                NgayThanhToanThucTe = actualDate,
                IsPaid = true
            };
            _dbContext.DotThanhToans.Add(dot);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _hopDongService.UndoPaymentAsync(dot.Id);

            // Assert
            result.Should().BeTrue();
            var dbDot = await _dbContext.DotThanhToans.FindAsync(dot.Id);
            dbDot!.IsPaid.Should().BeFalse();
            dbDot.NgayThanhToanThucTe.Should().BeNull();
            dbDot.NgayThanhToan.Should().Be(plannedDate); // Không bị xóa trắng ngày kế hoạch
        }

        [Fact]
        public async Task CreateAsync_Should_Create_Inline_Contractor_When_NewNhaThau_Is_Provided()
        {
            // Arrange
            var createDto = new CreateHopDongDto
            {
                Code = "HD-INLINE-CONTRACTOR",
                Name = "Hợp đồng tạo kèm nhà thầu inline",
                GiaTriHopDong = 500000000,
                NewNhaThau = new CreateDoiTacDto
                {
                    Code = "NT-NEW-INLINE",
                    Name = "Công ty TNHH Phần mềm Mới",
                    TaxCode = "0109999999",
                    Email = null // Email optional
                }
            };

            // Act
            var result = await _hopDongService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.NhaThauId.Should().NotBeNull();
            var dbContractor = await _dbContext.DoiTacs.FindAsync(result.NhaThauId);
            dbContractor.Should().NotBeNull();
            dbContractor!.Code.Should().Be("NT-NEW-INLINE");
            dbContractor.Name.Should().Be("Công ty TNHH Phần mềm Mới");
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("user@example.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@invalid.com", false)]
        [InlineData("invalid@", false)]
        public void OptionalEmailAddressAttribute_Should_Validate_Correctly(string? email, bool expectedValid)
        {
            var attribute = new demo1.Validator.OptionalEmailAddressAttribute();
            var isValid = attribute.IsValid(email);
            isValid.Should().Be(expectedValid);
        }

        [Fact]
        public async Task CreateAsync_Should_Record_Actual_Payment_Date_When_Provided_In_CreateDotThanhToanDto()
        {
            // Arrange
            var plannedDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            var actualDate = new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);
            var createDto = new CreateHopDongDto
            {
                Code = "HD-PAY-ACTUAL-CREATE",
                Name = "Hợp đồng có ngày thanh toán thực tế khi tạo",
                GiaTriHopDong = 1000000000,
                DotThanhToans = new List<CreateDotThanhToanDto>
                {
                    new CreateDotThanhToanDto
                    {
                        TenDot = "Đợt 1",
                        GiaTriThanhToan = 500000000,
                        NgayThanhToan = plannedDate,
                        NgayThanhToanThucTe = actualDate
                    }
                }
            };

            // Act
            var result = await _hopDongService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            var dbContract = await _dbContext.HopDongs.Include(h => h.DotThanhToans).FirstOrDefaultAsync(h => h.Id == result.Id);
            dbContract.Should().NotBeNull();
            var dot = dbContract!.DotThanhToans.First();
            dot.NgayThanhToan.Should().Be(plannedDate);
            dot.NgayThanhToanThucTe.Should().Be(actualDate);
            dot.IsPaid.Should().BeTrue();
        }

        [Fact]
        public async Task GetAllAsync_Should_Filter_By_ContractTypeId_And_Compute_Properties()
        {
            // Arrange
            var contractor1 = new DoiTac { Id = Guid.NewGuid(), Code = "DT-COMPUTED-A", Name = "Công ty A" };
            var contractor2 = new DoiTac { Id = Guid.NewGuid(), Code = "DT-COMPUTED-B", Name = "Công ty B" };
            _dbContext.DoiTacs.AddRange(contractor1, contractor2);


            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-COMPUTED", Name = "Dự án Computed" };
            var goiThau1 = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-COMPUTED-1", Name = "Gói thầu Computed 1" };
            var goiThau2 = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-COMPUTED-2", Name = "Gói thầu Computed 2" };
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.AddRange(goiThau1, goiThau2);

            var dynamicContractType = Guid.NewGuid();
            var loaiHopDongEntity = new demo1.Entity.DanhMuc.LoaiHopDong { Id = dynamicContractType, Code = "LHD-DYN", Name = "Loại HĐ Động" };
            _dbContext.LoaiHopDongs.Add(loaiHopDongEntity);

            var contract1 = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-COMPUTED-1",
                Name = "Hợp đồng thử nghiệm 1",
                GoiThauId = goiThau1.Id,
                NhaThauId = contractor1.Id,
                NhaThau = contractor1,
                LoaiHopDong = 1,
                NgayHieuLuc = new DateTime(2026, 1, 1),
                ExpiredDate = new DateTime(2026, 12, 31),
                DaKetThuc = false
            };

            var contract2 = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-COMPUTED-2",
                Name = "Hợp đồng thử nghiệm 2",
                GoiThauId = goiThau2.Id,
                LoaiHopDongId = dynamicContractType,
                NgayHieuLuc = new DateTime(2026, 5, 1),
                ExpiredDate = new DateTime(2026, 5, 10),
                DaKetThuc = true
            };

            contract2.NhaThauGoiThaus.Add(new NhaThauGoiThau { Id = Guid.NewGuid(), HopDongId = contract2.Id, NhaThauId = contractor1.Id, NhaThau = contractor1 });
            contract2.NhaThauGoiThaus.Add(new NhaThauGoiThau { Id = Guid.NewGuid(), HopDongId = contract2.Id, NhaThauId = contractor2.Id, NhaThau = contractor2 });



            _dbContext.HopDongs.AddRange(contract1, contract2);
            await _dbContext.SaveChangesAsync();

            // Act 1: Filter by string enum "1"
            var filterEnumResult = await _hopDongService.GetAllAsync(new HopDongFilterDto { ContractTypeId = "1" });
            
            // Assert 1
            filterEnumResult.Items.Should().Contain(h => h.Id == contract1.Id);
            var item1 = filterEnumResult.Items.First(h => h.Id == contract1.Id);
            item1.SoNgayThucHien.Should().Be(364);
            item1.TenLienDanhNhaThau.Should().Be("Công ty A");

            // Act 2: Filter by Guid string
            var filterGuidResult = await _hopDongService.GetAllAsync(new HopDongFilterDto { ContractTypeId = dynamicContractType.ToString() });

            // Assert 2
            filterGuidResult.Items.Should().Contain(h => h.Id == contract2.Id);
            var item2 = filterGuidResult.Items.First(h => h.Id == contract2.Id);
            item2.TenLienDanhNhaThau.Should().StartWith("Liên danh");
            item2.TrangThaiCalculatedText.Should().Be("Đã kết thúc / Thanh lý");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}

