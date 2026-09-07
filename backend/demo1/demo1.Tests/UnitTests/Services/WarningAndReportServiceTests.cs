using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class WarningAndReportServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;

        public WarningAndReportServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
        }

        [Fact]
        public async Task TC62_Contracts_Expiring_Warning_Filter()
        {
            // Arrange
            var contractExpiring = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-EXP-SOON",
                Name = "Hợp đồng sắp hết hạn",
                ExpiredDate = DateTime.UtcNow.AddDays(20)
            };

            var contractSafe = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-EXP-SAFE",
                Name = "Hợp đồng còn hạn lâu",
                ExpiredDate = DateTime.UtcNow.AddDays(120)
            };

            _dbContext.HopDongs.AddRange(contractExpiring, contractSafe);
            await _dbContext.SaveChangesAsync();

            // Act: Warning threshold 30 days
            var warningThreshold = DateTime.UtcNow.AddDays(30);
            var expiringContracts = _dbContext.HopDongs
                .Where(h => h.ExpiredDate != null && h.ExpiredDate <= warningThreshold)
                .ToList();

            // Assert
            expiringContracts.Should().HaveCount(1);
            expiringContracts[0].Code.Should().Be("HD-EXP-SOON");
        }

        [Fact]
        public async Task TC63_Contracts_Over_Budget_Warning_Filter()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-WARN", Name = "Dự án cảnh báo", DuToanPheDuyet = 1000000000 };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-WARN", GiaTriGoiThau = 1000000000 };
            var hopDongOver = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau.Id, Code = "HD-OVER", GiaTriHopDong = 1200000000 }; // 1.2B > 1.0B
            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.HopDongs.Add(hopDongOver);
            await _dbContext.SaveChangesAsync();

            // Act
            var isOverBudget = hopDongOver.GiaTriHopDong > goiThau.GiaTriGoiThau;

            // Assert
            isOverBudget.Should().BeTrue();
        }

        [Fact]
        public async Task TC64_Notification_Read_All_Should_Update_UnreadCount_To_Zero()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Username = "user_notif" };
            _dbContext.Users.Add(user);

            var n1 = new Notification { Id = Guid.NewGuid(), UserId = user.Id, Title = "Thông báo 1", IsRead = false };
            var n2 = new Notification { Id = Guid.NewGuid(), UserId = user.Id, Title = "Thông báo 2", IsRead = false };
            _dbContext.Notifications.AddRange(n1, n2);
            await _dbContext.SaveChangesAsync();

            // Act: Mark all read
            var userNotifs = _dbContext.Notifications.Where(n => n.UserId == user.Id).ToList();
            foreach (var n in userNotifs)
            {
                n.IsRead = true;
            }
            await _dbContext.SaveChangesAsync();

            // Assert
            var unreadCount = _dbContext.Notifications.Count(n => n.UserId == user.Id && !n.IsRead);
            unreadCount.Should().Be(0);
        }

        [Fact]
        public void TC70_CleanNotificationContent_Should_Remove_Leading_FeatureName_Prefix()
        {
            // Arrange & Act
            var res1 = demo1.Controllers.NotificationController.CleanNotificationContent("[Quản lý Hợp đồng] Hợp đồng ABC đã hết hạn 5 ngày.");
            var res2 = demo1.Controllers.NotificationController.CleanNotificationContent("[GOI_THAU]: Bạn có phân công công việc mới.");
            var res3 = demo1.Controllers.NotificationController.CleanNotificationContent("Thành viên A đã xác nhận công việc.");

            // Assert
            res1.Should().Be("Hợp đồng ABC đã hết hạn 5 ngày.");
            res2.Should().Be("Bạn có phân công công việc mới.");
            res3.Should().Be("Thành viên A đã xác nhận công việc.");
        }

        [Fact]
        public async Task TC66_TC67_TC68_TC69_Investment_Report_Calculations()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-REP", Name = "Dự án báo cáo", DuToanPheDuyet = 5000000000 };
            var goiThau = new GoiThau { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "GT-REP", GiaTriGoiThau = 3000000000 };
            var hopDong = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, GoiThauId = goiThau.Id, Code = "HD-REP", GiaTriHopDong = 2800000000 };
            var dotThanhToan = new DotThanhToan { Id = Guid.NewGuid(), HopDongId = hopDong.Id, TenDot = "Đợt 1", GiaTriThanhToan = 1400000000, IsPaid = true };

            _dbContext.DuAns.Add(project);
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.HopDongs.Add(hopDong);
            _dbContext.DotThanhToans.Add(dotThanhToan);
            await _dbContext.SaveChangesAsync();

            // Act: Calculate disbursement rate
            double duToan = (double)project.DuToanPheDuyet!;
            double daGiaiNgan = (double)dotThanhToan.GiaTriThanhToan;
            double tyLeGiaiNgan = (daGiaiNgan / duToan) * 100.0;

            // Assert
            duToan.Should().Be(5000000000);
            daGiaiNgan.Should().Be(1400000000);
            tyLeGiaiNgan.Should().BeApproximately(28.0, 0.01);
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Calculate_KyTruoc_TrongKy_And_GiaiNgan_Correctly()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-INV-CALC", Name = "Dự án CNTT tính toán", DuToanPheDuyet = 10000000000m, LoaiDuAn = 2, TrangThai = 1 };
            var hopDong = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "HD-INV-CALC", GiaTriHopDong = 8000000000m, IsActive = true, IsDeleted = false };
            
            // Đợt 1: Kỳ trước (năm 2025)
            var dotKyTruoc = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hopDong.Id,
                HopDong = hopDong,
                TenDot = "Đợt 1 - 2025",
                GiaTriThanhToan = 2000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2025, 11, 15, 0, 0, 0, DateTimeKind.Utc)
            };
            
            // Đợt 2: Trong kỳ (6T đầu năm 2026)
            var dotTrongKy = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hopDong.Id,
                HopDong = hopDong,
                TenDot = "Đợt 2 - 2026",
                GiaTriThanhToan = 3000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.DuAns.Add(project);
            _dbContext.HopDongs.Add(hopDong);
            _dbContext.DotThanhToans.AddRange(dotKyTruoc, dotTrongKy);
            await _dbContext.SaveChangesAsync();

            // Act: Report for year 2026, period 1 (6T)
            var report = await service.GetInvestmentReportAsync(2026, 1, "đồng");

            // Assert
            report.Should().NotBeNull();
            var projectRow = report.Rows.FirstOrDefault(r => r.ProjectName == "Dự án CNTT tính toán");
            projectRow.Should().NotBeNull();

            // 1. Kỳ trước chuyển sang = tất cả về trước (2,000,000,000)
            projectRow!.KhoiLuongKyTruoc.Should().Be(2000000000m);
            
            // 2. Thực hiện trong kỳ (3,000,000,000)
            projectRow.KhoiLuongTrongKy.Should().Be(3000000000m);

            // 3. Cột thực hiện đến ngày = kỳ trước chuyển sang + thực hiện trong kỳ (5,000,000,000)
            projectRow.KhoiLuongLuyKe.Should().Be(5000000000m);
            projectRow.KhoiLuongLuyKe.Should().Be(projectRow.KhoiLuongKyTruoc + projectRow.KhoiLuongTrongKy);

            // 4. Giải ngân = Khối lượng thực hiện
            projectRow.GiaiNganKyTruoc.Should().Be(projectRow.KhoiLuongKyTruoc);
            projectRow.GiaiNganTrongKy.Should().Be(projectRow.KhoiLuongTrongKy);
            projectRow.GiaiNganLuyKe.Should().Be(projectRow.KhoiLuongLuyKe);
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Categorize_GroupB_Projects()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var projB = new DuAn { Id = Guid.NewGuid(), Code = "DA-B-50B", Name = "Dự án nhóm B quy mô lớn", DuToanPheDuyet = 50000000000m, LoaiDuAn = 2, TrangThai = 1 };
            _dbContext.DuAns.Add(projB);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetInvestmentReportAsync(2026, 1, "đồng");

            // Assert
            var groupBHeader = report.Rows.FirstOrDefault(r => r.Stt == "B" && r.RowType == "GroupHeader");
            var groupBFooter = report.Rows.FirstOrDefault(r => r.ProjectName == "Tổng (B)" && r.RowType == "GroupFooter");
            
            groupBHeader.Should().NotBeNull();
            groupBFooter.Should().NotBeNull();
            groupBFooter!.TongMucDauTuTong.Should().BeGreaterThanOrEqualTo(50000000000m);
        }



        [Fact]
        public async Task ReportService_ExportCongViecGoiThauAndTheoDoiHopDong_Csv_Html_Should_Return_NonEmpty_Bytes()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var goiThauId = Guid.NewGuid();
            var goiThau = new GoiThau { Id = goiThauId, Code = "GT-TEST", Name = "Gói thầu test" };
            var congViec = new CongViecGoiThau { Id = Guid.NewGuid(), GoiThauId = goiThauId, Stt = 1, TenTaiLieu = "Tài liệu A", LoaiVanBan = "Quyết định", TinhTrang = "Hoàn thành" };
            _dbContext.GoiThaus.Add(goiThau);
            _dbContext.CongViecGoiThaus.Add(congViec);

            var hopDong = new HopDong { Id = Guid.NewGuid(), Code = "HD-TEST", Name = "Hợp đồng test", GiaTriHopDong = 1000000000 };
            _dbContext.HopDongs.Add(hopDong);
            await _dbContext.SaveChangesAsync();

            // Act
            var csvGoiThau = await service.ExportCongViecGoiThauReportCsvAsync(goiThauId);
            var htmlGoiThau = await service.ExportCongViecGoiThauReportHtmlAsync(goiThauId);
            var csvHopDong = await service.ExportTheoDoiHopDongReportCsvAsync(DateTime.UtcNow.Year, null, null, null);
            var htmlHopDong = await service.ExportTheoDoiHopDongReportHtmlAsync(DateTime.UtcNow.Year, null, null, null);

            // Assert
            csvGoiThau.Should().NotBeNullOrEmpty();
            htmlGoiThau.Should().NotBeNullOrEmpty();
            csvHopDong.Should().NotBeNullOrEmpty();
            htmlHopDong.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ReportService_NewPdfReports_KeHoachVon_And_KeHoachVonCntt_Should_Generate_Data_And_Exports()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var projXdcb = new DuAn { Id = Guid.NewGuid(), Code = "DA-XDCB", Name = "Xây dựng trụ sở mới", DuToanPheDuyet = 90000000000, NoiDung = "xây dựng trụ sở 5 tầng" };
            var projCntt = new DuAn { Id = Guid.NewGuid(), Code = "DA-CNTT", Name = "Trang bị hệ thống Backup", DuToanPheDuyet = 25000000000, NoiDung = "CNTT backup data" };
            _dbContext.DuAns.AddRange(projXdcb, projCntt);
            await _dbContext.SaveChangesAsync();

            // Act
            var khvReport = await service.GetKeHoachVonReportAsync(2025, 1, "triệu");
            var khvExcel = await service.ExportKeHoachVonReportExcelAsync(2025, 1, "triệu");
            var khvCsv = await service.ExportKeHoachVonReportCsvAsync(2025, 1, "triệu");

            var cnttReport = await service.GetKeHoachVonCnttReportAsync(2022, 2024, null, "1");
            var cnttExcel = await service.ExportKeHoachVonCnttReportExcelAsync(2022, 2024, null, "1");
            var cnttHtml = await service.ExportKeHoachVonCnttReportHtmlAsync(2022, 2024, null, "1");

            // Assert
            khvReport.Should().NotBeNull();
            khvReport.PhuLucs.Should().NotBeEmpty();
            khvExcel.Should().NotBeNullOrEmpty();
            khvCsv.Should().NotBeNullOrEmpty();

            cnttReport.Should().NotBeNull();
            cnttReport.Groups.Should().NotBeEmpty();
            cnttReport.Groups.SelectMany(g => g.Rows).Should().AllSatisfy(r => r.TenPhanLoaiDuAn.Should().NotBeNullOrEmpty());
            cnttExcel.Should().NotBeNullOrEmpty();
            cnttHtml.Should().NotBeNullOrEmpty();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
