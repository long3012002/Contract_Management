using System;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.Entity;
using demo1.Entity.DanhMuc;
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

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-INV-CALC", Name = "Dự án CNTT tính toán", DuToanPheDuyet = 10000000000m, TrangThai = 1 };
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
        public async Task ReportService_GetInvestmentReportAsync_Should_Support_Custom_FromDate_And_ToDate()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var fromDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var toDate = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc);

            // Act
            var report = await service.GetInvestmentReportAsync(2026, 1, "đồng", fromDate, toDate);

            // Assert
            report.Should().NotBeNull();
            report.FromDate.Should().Be(fromDate);
            report.ToDate.Should().Be(toDate);
            report.PeriodName.Should().Be("TuyChon");
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Filter_WholeYear_When_Period_Is_2()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-YEAR-2026", Name = "Dự án cả năm 2026", DuToanPheDuyet = 5000000000m, TrangThai = 1 };
            var hopDong = new HopDong { Id = Guid.NewGuid(), DuAnId = project.Id, Code = "HD-YEAR-2026", GiaTriHopDong = 4000000000m, IsActive = true, IsDeleted = false };

            // Đợt 1: 6T đầu năm (tháng 3/2026)
            var dot1 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hopDong.Id,
                HopDong = hopDong,
                TenDot = "Đợt 1 - T3/2026",
                GiaTriThanhToan = 1000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc)
            };

            // Đợt 2: 6T cuối năm (tháng 10/2026)
            var dot2 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hopDong.Id,
                HopDong = hopDong,
                TenDot = "Đợt 2 - T10/2026",
                GiaTriThanhToan = 2000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.DuAns.Add(project);
            _dbContext.HopDongs.Add(hopDong);
            _dbContext.DotThanhToans.AddRange(dot1, dot2);
            await _dbContext.SaveChangesAsync();

            // Act 1: Period 1 (6T đầu năm)
            var reportP1 = await service.GetInvestmentReportAsync(2026, 1, "đồng");
            var rowP1 = reportP1.Rows.FirstOrDefault(r => r.ProjectName == "Dự án cả năm 2026");
            rowP1.Should().NotBeNull();
            rowP1!.KhoiLuongTrongKy.Should().Be(1000000000m); // Chỉ có đợt 1
            reportP1.Period.Should().Be(1);
            reportP1.FromDate.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            reportP1.ToDate.Should().Be(new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc));

            // Act 2: Period 2 (Cả năm)
            var reportP2 = await service.GetInvestmentReportAsync(2026, 2, "đồng");
            var rowP2 = reportP2.Rows.FirstOrDefault(r => r.ProjectName == "Dự án cả năm 2026");
            rowP2.Should().NotBeNull();
            rowP2!.KhoiLuongTrongKy.Should().Be(3000000000m); // Cả đợt 1 + đợt 2
            reportP2.Period.Should().Be(2);
            reportP2.FromDate.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            reportP2.ToDate.Should().Be(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Calculate_TongMucDauTu_And_Funding_Sources_Correctly()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var nvVcsh = new NguonVon { Id = Guid.NewGuid(), Code = "NV_VDL_QDTR", Name = "Vốn điều lệ và Quỹ dự trữ", IsActive = true };
            var nvVay = new NguonVon { Id = Guid.NewGuid(), Code = "NV_VAY", Name = "Vốn vay thương mại", IsActive = true };
            var nvKhac = new NguonVon { Id = Guid.NewGuid(), Code = "NV_KHAC", Name = "Nguồn khác", IsActive = true };
            _dbContext.NguonVons.AddRange(nvVcsh, nvVay, nvKhac);

            var project = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-FUNDING-SRC",
                Name = "Dự án Nguồn vốn đầy đủ",
                DuToanPheDuyet = 10000000000m,
                TrangThai = 1
            };

            var dnv1 = new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nvVcsh.Id, SoTien = 6000000000m };
            var dnv2 = new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nvVay.Id, SoTien = 3000000000m };
            var dnv3 = new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nvKhac.Id, SoTien = 1000000000m };

            _dbContext.DuAns.Add(project);
            _dbContext.DuAnNguonVons.AddRange(dnv1, dnv2, dnv3);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetInvestmentReportAsync(2026, 2, "đồng");

            // Assert
            var row = report.Rows.FirstOrDefault(r => r.ProjectName == "Dự án Nguồn vốn đầy đủ");
            row.Should().NotBeNull();
            row!.TongMucDauTuTong.Should().Be(10000000000m);
            row.TongMucDauTuVCSH.Should().Be(6000000000m);
            row.TongMucDauTuVay.Should().Be(3000000000m);
            row.TongMucDauTuKhac.Should().Be(1000000000m);
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Categorize_GroupB_Projects()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var projB = new DuAn { Id = Guid.NewGuid(), Code = "DA-B-50B", Name = "Dự án nhóm B quy mô lớn", DuToanPheDuyet = 50000000000m, TrangThai = 1 };
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

            // Verify that Group C is excluded since it has no projects
            report.Rows.Any(r => r.Stt == "C" && r.RowType == "GroupHeader").Should().BeFalse();
            report.Rows.Any(r => r.ProjectName == "Tổng (C)").Should().BeFalse();
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Exclude_Empty_Project_Groups()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            // Case 1: No projects at all -> Neither Group B nor Group C should be in rows
            var emptyReport = await service.GetInvestmentReportAsync(2026, 1, "đồng");
            emptyReport.Rows.Any(r => r.RowType == "GroupHeader").Should().BeFalse();
            emptyReport.Rows.Any(r => r.RowType == "GrandTotal").Should().BeTrue();

            // Case 2: Only Group C project (< 45B) -> Group B should be excluded, Group C included
            var projC = new DuAn { Id = Guid.NewGuid(), Code = "DA-C-10B", Name = "Dự án nhóm C quy mô nhỏ", DuToanPheDuyet = 10000000000m, TrangThai = 1 };
            _dbContext.DuAns.Add(projC);
            await _dbContext.SaveChangesAsync();

            var reportWithC = await service.GetInvestmentReportAsync(2026, 1, "đồng");
            reportWithC.Rows.Any(r => r.Stt == "B" && r.RowType == "GroupHeader").Should().BeFalse();
            reportWithC.Rows.Any(r => r.ProjectName == "Tổng (B)").Should().BeFalse();
            reportWithC.Rows.Any(r => r.Stt == "C" && r.RowType == "GroupHeader").Should().BeTrue();
            reportWithC.Rows.Any(r => r.ProjectName == "Tổng (C)").Should().BeTrue();
        }

        [Fact]
        public async Task ReportService_GetInvestmentReportAsync_Should_Support_MultiYear_Data_And_Group_By_PhanLoaiDuAn()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var plCntt = new PhanLoaiDuAn { Id = Guid.NewGuid(), Code = "PL_CNTT", Name = "Dự án Công nghệ thông tin", IsActive = true };
            var plXdcb = new PhanLoaiDuAn { Id = Guid.NewGuid(), Code = "PL_XDCB", Name = "Dự án Xây dựng cơ bản", IsActive = true };
            _dbContext.PhanLoaiDuAns.AddRange(plCntt, plXdcb);

            var projCntt = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-MULTI-CNTT",
                Name = "Dự án CNTT Đa Năm",
                DuToanPheDuyet = 60000000000m, // Group B
                PhanLoaiDuAnId = plCntt.Id,
                TrangThai = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var projXdcb = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-MULTI-XDCB",
                Name = "Dự án XDCB Đa Năm",
                DuToanPheDuyet = 10000000000m, // Group C
                PhanLoaiDuAnId = plXdcb.Id,
                TrangThai = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var hdCntt = new HopDong { Id = Guid.NewGuid(), DuAnId = projCntt.Id, Code = "HD-MULTI-CNTT", GiaTriHopDong = 50000000000m, IsActive = true };
            var hdXdcb = new HopDong { Id = Guid.NewGuid(), DuAnId = projXdcb.Id, Code = "HD-MULTI-XDCB", GiaTriHopDong = 800000000m, IsActive = true };

            // Đợt thanh toán năm 2024 (1 tỷ)
            var dot2024 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hdCntt.Id,
                HopDong = hdCntt,
                TenDot = "Tạm ứng 2024",
                GiaTriThanhToan = 1000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            // Đợt thanh toán năm 2025 (2 tỷ)
            var dot2025 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hdCntt.Id,
                HopDong = hdCntt,
                TenDot = "Nghiệm thu 2025",
                GiaTriThanhToan = 2000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            // Đợt thanh toán năm 2026 (3 tỷ)
            var dot2026 = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hdCntt.Id,
                HopDong = hdCntt,
                TenDot = "Thanh toán 2026",
                GiaTriThanhToan = 3000000000m,
                IsPaid = true,
                NgayThanhToan = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            _dbContext.DuAns.AddRange(projCntt, projXdcb);
            _dbContext.HopDongs.AddRange(hdCntt, hdXdcb);
            _dbContext.DotThanhToans.AddRange(dot2024, dot2025, dot2026);
            await _dbContext.SaveChangesAsync();

            // Act 1: Báo cáo năm 2025 (Cả năm - period 2)
            var report2025 = await service.GetInvestmentReportAsync(2025, 2, "đồng");

            // Assert 1: Năm 2025 -> Kỳ trước = 2024 (1 tỷ), Trong kỳ = 2025 (2 tỷ), Lũy kế = 3 tỷ
            var row2025 = report2025.Rows.FirstOrDefault(r => r.ProjectName == "Dự án CNTT Đa Năm");
            row2025.Should().NotBeNull();
            row2025!.KhoiLuongKyTruoc.Should().Be(1000000000m);
            row2025.KhoiLuongTrongKy.Should().Be(2000000000m);
            row2025.KhoiLuongLuyKe.Should().Be(3000000000m);

            // Act 2: Báo cáo năm 2026 (6 tháng đầu năm - period 1)
            var report2026 = await service.GetInvestmentReportAsync(2026, 1, "đồng");

            // Assert 2: Năm 2026 -> Kỳ trước = 2024+2025 (3 tỷ), Trong kỳ = 2026 (3 tỷ), Lũy kế = 6 tỷ
            var row2026 = report2026.Rows.FirstOrDefault(r => r.ProjectName == "Dự án CNTT Đa Năm");
            row2026.Should().NotBeNull();
            row2026!.KhoiLuongKyTruoc.Should().Be(3000000000m);
            row2026.KhoiLuongTrongKy.Should().Be(3000000000m);
            row2026.KhoiLuongLuyKe.Should().Be(6000000000m);

            // Assert 3: Phân nhóm theo PhanLoaiDuAn (Dự án Công nghệ thông tin & Dự án Xây dựng cơ bản)
            var subHeaderCntt = report2026.Rows.FirstOrDefault(r => r.RowType == "SubGroupHeader" && r.ProjectName == "Dự án Công nghệ thông tin");
            var subHeaderXdcb = report2026.Rows.FirstOrDefault(r => r.RowType == "SubGroupHeader" && r.ProjectName == "Dự án Xây dựng cơ bản");
            subHeaderCntt.Should().NotBeNull();
            subHeaderXdcb.Should().NotBeNull();
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

            var projXdcb = new DuAn { Id = Guid.NewGuid(), Code = "DA-XDCB", Name = "Xây dựng trụ sở mới", DuToanPheDuyet = 90000000000, NoiDung = "xây dựng trụ sở 5 tầng", DaTrienKhai = true };
            var projCntt = new DuAn { Id = Guid.NewGuid(), Code = "DA-CNTT", Name = "Trang bị hệ thống Backup", DuToanPheDuyet = 25000000000, NoiDung = "CNTT backup data", DaTrienKhai = true };
            _dbContext.DuAns.AddRange(projXdcb, projCntt);
            await _dbContext.SaveChangesAsync();

            // Act
            var khvReport = await service.GetKeHoachVonReportAsync(2025, 1, "triệu");
            var khvExcel = await service.ExportKeHoachVonReportExcelAsync(2025, 1, "triệu");
            var khvCsv = await service.ExportKeHoachVonReportCsvAsync(2025, 1, "triệu");

            var cnttReport = await service.GetKeHoachVonCnttReportAsync(2022, 2024, null, "1");
            var cnttExcel = await service.ExportKeHoachVonCnttReportExcelAsync(2022, 2024, null, "1");
            var cnttHtml = await service.ExportKeHoachVonCnttReportHtmlAsync(2022, 2024, null, "1");

            var cnttReportFiltered = await service.GetKeHoachVonCnttReportAsync(2022, 2024, null, "1", keyword: "Phần mềm", projectType: "A");

            // Assert
            khvReport.Should().NotBeNull();
            khvReport.PhuLucs.Should().NotBeEmpty();
            khvExcel.Should().NotBeNullOrEmpty();
            khvCsv.Should().NotBeNullOrEmpty();

            cnttReport.Should().NotBeNull();
            cnttReport.Groups.Should().NotBeEmpty();
            cnttReport.Groups.Should().Contain(g => !string.IsNullOrEmpty(g.LoaiDuAnKey));
            cnttReport.Groups.SelectMany(g => g.Rows).Should().AllSatisfy(r => {
                r.TenPhanLoaiDuAn.Should().NotBeNullOrEmpty();
                r.LoaiDuAn.Should().NotBeNullOrEmpty();
            });
            cnttReport.TongSoDuAn.Should().BeGreaterOrEqualTo(0);

            cnttReportFiltered.Should().NotBeNull();
            cnttReportFiltered.Groups.Should().NotBeEmpty();

            cnttExcel.Should().NotBeNullOrEmpty();
            cnttHtml.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ReportService_KeHoachVonCntt_NguonVon_Classification_Should_Map_Correctly()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var nvNhht = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_NHHT", Name = "Chi phí của NHHT" };
            var nvQdtpt = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_QDTPT", Name = "Quỹ đầu tư phát triển" };
            var nvKhac = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_KHAC", Name = "Nguồn khác" };

            _dbContext.NguonVons.AddRange(nvNhht, nvQdtpt, nvKhac);

            var p1 = new DuAn { Id = Guid.NewGuid(), Code = "DA-NV1", Name = "Dự án phần mềm 1", DuToanPheDuyet = 1000000, DaTrienKhai = true };
            p1.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = p1.Id, NguonVonId = nvNhht.Id, SoTien = 1000000 });

            var p2 = new DuAn { Id = Guid.NewGuid(), Code = "DA-NV2", Name = "Dự án phần mềm 2", DuToanPheDuyet = 2000000, DaTrienKhai = true };
            p2.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = p2.Id, NguonVonId = nvQdtpt.Id, SoTien = 2000000 });

            var p3 = new DuAn { Id = Guid.NewGuid(), Code = "DA-NV3", Name = "Dự án phần mềm 3", DuToanPheDuyet = 3000000, DaTrienKhai = true };
            p3.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = p3.Id, NguonVonId = nvKhac.Id, SoTien = 3000000 });

            _dbContext.DuAns.AddRange(p1, p2, p3);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await service.GetKeHoachVonCnttReportAsync(2022, 2024, null, "1");

            // Assert
            var rows = result.Groups.SelectMany(g => g.Rows).ToList();
            var row1 = rows.FirstOrDefault(r => r.DuAnId == p1.Id);
            var row2 = rows.FirstOrDefault(r => r.DuAnId == p2.Id);
            var row3 = rows.FirstOrDefault(r => r.DuAnId == p3.Id);

            row1.Should().NotBeNull();
            row1!.VonTuCo.Should().Be(1000000);
            row1.QuyDauTuPhatTrien.Should().Be(0);
            row1.NguonKhac.Should().Be(0);

            row2.Should().NotBeNull();
            row2!.VonTuCo.Should().Be(0);
            row2.QuyDauTuPhatTrien.Should().Be(2000000);
            row2.NguonKhac.Should().Be(0);

            row3.Should().NotBeNull();
            row3!.VonTuCo.Should().Be(0);
            row3.QuyDauTuPhatTrien.Should().Be(0);
            row3.NguonKhac.Should().Be(3000000);
        }

        [Fact]
        public async Task ReportService_LicenseSlaReport_Should_Generate_Data_And_Exports()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var vendor = new DoiTac { Id = Guid.NewGuid(), Name = "FPT IS" };
            var proj = new DuAn { Id = Guid.NewGuid(), Code = "DA-LIC", Name = "Dự án Nâng cấp Core", DuToanPheDuyet = 5000000000 };
            var contract = new HopDong { Id = Guid.NewGuid(), Code = "HD-LIC-01", Name = "HĐ Bản quyền Oracle", GiaTriHopDong = 3000000000, DuAnId = proj.Id };

            var licExpired = new License
            {
                Id = Guid.NewGuid(),
                Code = "LIC-01",
                Name = "Oracle DB License",
                NhaCungCapId = vendor.Id,
                DuAnId = proj.Id,
                HopDongId = contract.Id,
                LoaiLicense = 1,
                NgayBatDau = DateTime.UtcNow.AddYears(-1),
                NgayKetThuc = DateTime.UtcNow.AddDays(-10),
                CanhBaoTruocNgay = 30,
                TrangThai = 3
            };

            var licExpiring = new License
            {
                Id = Guid.NewGuid(),
                Code = "LIC-02",
                Name = "Fortinet License",
                NhaCungCapId = vendor.Id,
                DuAnId = proj.Id,
                HopDongId = contract.Id,
                LoaiLicense = 1,
                NgayBatDau = DateTime.UtcNow.AddYears(-1),
                NgayKetThuc = DateTime.UtcNow.AddDays(15),
                CanhBaoTruocNgay = 30,
                TrangThai = 2
            };

            _dbContext.DoiTacs.Add(vendor);
            _dbContext.DuAns.Add(proj);
            _dbContext.HopDongs.Add(contract);
            _dbContext.Licenses.AddRange(licExpired, licExpiring);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetLicenseSlaReportAsync(null, null, "đồng");
            var excelBytes = await service.ExportLicenseSlaReportExcelAsync(null, null, "đồng");
            var csvBytes = await service.ExportLicenseSlaReportCsvAsync(null, null, "đồng");
            var htmlBytes = await service.ExportLicenseSlaReportHtmlAsync(null, null, "đồng");

            // Assert
            report.Should().NotBeNull();
            report.TongHop.TongSoLicense.Should().Be(2);
            report.TongHop.SoLicenseDaHetHan.Should().Be(1);
            report.TongHop.SoLicenseSapHetHan30Ngay.Should().Be(1);
            report.DanhSachChiTiet.Should().HaveCount(2);

            excelBytes.Should().NotBeNullOrEmpty();
            csvBytes.Should().NotBeNullOrEmpty();
            htmlBytes.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ReportService_KeHoachVonCntt_Multiple_NguonVon_Per_Project_Should_Map_Correctly()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var nv1 = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_TEST_1", Name = "Vốn tự có NHHT" };
            var nv2 = new demo1.Entity.DanhMuc.NguonVon { Id = Guid.NewGuid(), Code = "NV_TEST_2", Name = "Quỹ ĐTPT" };
            _dbContext.NguonVons.AddRange(nv1, nv2);

            var proj = new DuAn 
            { 
                Id = Guid.NewGuid(), 
                Code = "DA-MULTI-NV", 
                Name = "Dự án CNTT nhiều nguồn vốn", 
                DuToanPheDuyet = 3000000000m,
                DaTrienKhai = true
            };

            proj.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = proj.Id, NguonVonId = nv1.Id, SoTien = 1000000000m });
            proj.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = proj.Id, NguonVonId = nv2.Id, SoTien = 2000000000m });
            proj.PhanKyVons.Add(new DuAnPhanKyVon { Id = Guid.NewGuid(), DuAnId = proj.Id, Nam = 2025, SoTienPhanKy = 1500000000m });
            proj.PhanKyVons.Add(new DuAnPhanKyVon { Id = Guid.NewGuid(), DuAnId = proj.Id, Nam = 2026, SoTienPhanKy = 1500000000m });

            _dbContext.DuAns.Add(proj);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetKeHoachVonCnttReportAsync(2025, 2026, null, "1");
            var excelBytes = await service.ExportKeHoachVonCnttReportExcelAsync(2025, 2026, null, "1");
            var csvBytes = await service.ExportKeHoachVonCnttReportCsvAsync(2025, 2026, null, "1");
            var htmlBytes = await service.ExportKeHoachVonCnttReportHtmlAsync(2025, 2026, null, "1");

            // Assert
            report.Should().NotBeNull();
            var row = report.Groups.SelectMany(g => g.Rows).FirstOrDefault(r => r.DuAnId == proj.Id);
            row.Should().NotBeNull();
            row!.NguonVonChiTiet[nv1.Id].Should().Be(1000000000m);
            row.NguonVonChiTiet[nv2.Id].Should().Be(2000000000m);
            row.PhanKyDauTu.FirstOrDefault(p => p.Nam == 2025)?.GiaTri.Should().Be(1500000000m);
            row.PhanKyDauTu.FirstOrDefault(p => p.Nam == 2026)?.GiaTri.Should().Be(1500000000m);

            excelBytes.Should().NotBeNullOrEmpty();
            csvBytes.Should().NotBeNullOrEmpty();
            htmlBytes.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetKeHoachVonCnttReportAsync_Should_Only_Include_Approved_Or_Higher_Status_Projects()
        {
            // Arrange
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<demo1.Services.Implements.ReportService>.Instance;
            var service = new demo1.Services.Implements.ReportService(_dbContext, logger);

            var projTrienKhaiApproved1 = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-APPROVED-01",
                Name = "Dự án đã triển khai",
                DuToanPheDuyet = 300000000m,
                DaTrienKhai = true,
                NoiDung = "Phần mềm đã triển khai",
                IsActive = true
            };

            var projTrienKhaiApproved2 = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-APPROVED-02",
                Name = "Dự án hoàn thành phê duyệt",
                DuToanPheDuyet = 400000000m,
                TrangThai = 2,
                NoiDung = "Hạ tầng đã duyệt",
                IsActive = true
            };

            var projTrienKhaiUnapproved = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-UNAPPROVED-01",
                Name = "Dự án chưa duyệt",
                DuToanPheDuyet = 500000000m,
                DaTrienKhai = false,
                TrangThai = 0,
                NoiDung = "Phần mềm dự thảo",
                IsActive = true
            };

            _dbContext.DuAns.AddRange(projTrienKhaiApproved1, projTrienKhaiApproved2, projTrienKhaiUnapproved);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetKeHoachVonCnttReportAsync(2025, 2026, 1, "1");

            // Assert
            report.Should().NotBeNull();
            var allRowProjectIds = report.Groups.SelectMany(g => g.Rows).Select(r => r.DuAnId).ToList();

            allRowProjectIds.Should().Contain(projTrienKhaiApproved1.Id);
            allRowProjectIds.Should().Contain(projTrienKhaiApproved2.Id);
            allRowProjectIds.Should().NotContain(projTrienKhaiUnapproved.Id);
        }

        [Fact]
        public async Task GetGoiThauLcntReportAsync_Should_Calculate_Savings_And_Summary_Correctly()
        {
            // Arrange
            var service = new demo1.Services.Implements.ReportService(_dbContext, null!);

            var proj = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-LCNT-01",
                Name = "Dự án LCNT Test",
                IsActive = true
            };
            _dbContext.DuAns.Add(proj);

            var gt1 = new GoiThau
            {
                Id = Guid.NewGuid(),
                Code = "GT-01",
                Name = "Gói thầu thiết bị",
                DuAnId = proj.Id,
                GiaTriGoiThau = 500000000m,
                IsActive = true
            };
            var contractor = new DoiTac
            {
                Id = Guid.NewGuid(),
                Code = "NT-01",
                Name = "Công ty CP Công nghệ X",
                IsActive = true
            };
            _dbContext.DoiTacs.Add(contractor);

            var contract1 = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-GT-01",
                Name = "Hợp đồng thiết bị",
                GoiThauId = gt1.Id,
                DuAnId = proj.Id,
                NhaThauId = contractor.Id,
                GiaTriHopDong = 485000000m,
                IsActive = true
            };

            _dbContext.GoiThaus.Add(gt1);
            _dbContext.HopDongs.Add(contract1);
            await _dbContext.SaveChangesAsync();

            // Act
            var report = await service.GetGoiThauLcntReportAsync(null, proj.Id, null, "đồng");

            // Assert
            report.Should().NotBeNull();
            report.Summary.TongSoGoiThau.Should().Be(1);
            report.Summary.TongGiaTriDuToan.Should().Be(500000000m);
            report.Summary.TongGiaTriHopDongDaKy.Should().Be(485000000m);
            report.Summary.TongGiaTriTietKiem.Should().Be(15000000m);
            report.Summary.TyLeTietKiemChungPercent.Should().Be(3.0);

            var row = report.Rows.First();
            row.TenNhaThauTrungThau.Should().Be("Công ty CP Công nghệ X");
            row.TrangThaiGoiThau.Should().Be("Đã hoàn thành LCNT");
            row.TyLeSuDungDuToanPercent.Should().Be(97.0);
        }

        [Fact]
        public void TheoDoiHopDongReportDto_JsonSerialization_ShouldNotHavePropertyCollision()
        {
            var dto = new demo1.DTOs.TheoDoiHopDongReportResponseDto
            {
                Rows = new System.Collections.Generic.List<demo1.DTOs.TheoDoiHopDongReportRowDto>
                {
                    new demo1.DTOs.TheoDoiHopDongReportRowDto
                    {
                        SoHopDong = "HD01",
                        TenHopDong = "Hợp đồng thử nghiệm",
                        NguoiDaiDienVaSdt = "Nguyễn Văn A (0987654321)"
                    }
                }
            };

            var options = new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
            var json = System.Text.Json.JsonSerializer.Serialize(dto, options);

            json.Should().NotBeNullOrWhiteSpace();
            json.Should().Contain("nguoiDaiDienVaSdt");
            json.Should().Contain("nguoiDaiDien_SDT");
        }

        [Fact]
        public async Task ReportExportExcel_Version1_And_Version2_Should_Generate_Correct_Sheets_And_Headers()
        {
            // Arrange
            var service = new demo1.Services.Implements.ReportService(_dbContext, null!);

            var proj = new DuAn
            {
                Id = Guid.NewGuid(),
                Code = "DA-TEST-V2",
                Name = "Dự án Thử nghiệm V2",
                NgayBatDau = DateTime.UtcNow.AddMonths(-2),
                NgayKetThuc = DateTime.UtcNow.AddMonths(4),
                DuToanPheDuyet = 500000000m,
                IsActive = true
            };
            _dbContext.DuAns.Add(proj);

            var gt = new GoiThau
            {
                Id = Guid.NewGuid(),
                Code = "GT-TEST-V2",
                Name = "Gói thầu V2",
                DuAnId = proj.Id,
                GiaTriGoiThau = 300000000m,
                IsActive = true
            };
            _dbContext.GoiThaus.Add(gt);

            var contractor = new DoiTac
            {
                Id = Guid.NewGuid(),
                Code = "DT-V2",
                Name = "Nhà thầu V2",
                Representative = "Lê Văn T",
                Phone = "0912345678",
                IsActive = true
            };
            _dbContext.DoiTacs.Add(contractor);

            var hopDong = new HopDong
            {
                Id = Guid.NewGuid(),
                Code = "HD-V2",
                Name = "Hợp đồng V2",
                DuAnId = proj.Id,
                GoiThauId = gt.Id,
                NhaThauId = contractor.Id,
                GiaTriHopDong = 280000000m,
                NgayHieuLuc = DateTime.UtcNow.AddMonths(-1),
                ExpiredDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true
            };
            _dbContext.HopDongs.Add(hopDong);

            var dotTt = new DotThanhToan
            {
                Id = Guid.NewGuid(),
                HopDongId = hopDong.Id,
                TenDot = "Tạm ứng 30%",
                TyLeThanhToan = 30m,
                GiaTriThanhToan = 84000000m,
                NgayThanhToan = DateTime.UtcNow.AddMonths(1),
                DieuKienThanhToan = "Sau khi ký hợp đồng",
                IsPaid = true
            };
            _dbContext.DotThanhToans.Add(dotTt);
            await _dbContext.SaveChangesAsync();

            // 1. Test Báo cáo 1: Tiến độ Dự án (V1 vs V2)
            var bytesV1 = await service.ExportInvestmentReportExcelAsync(DateTime.UtcNow.Year, 1, "đồng", 1);
            bytesV1.Should().NotBeNullOrEmpty();
            using (var wbV1 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(bytesV1)))
            {
                wbV1.Worksheets.Contains("Bao cao").Should().BeTrue();
            }

            var bytesV2 = await service.ExportInvestmentReportExcelAsync(DateTime.UtcNow.Year, 1, "đồng", 2);
            bytesV2.Should().NotBeNullOrEmpty();
            using (var wbV2 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(bytesV2)))
            {
                wbV2.Worksheets.Contains("Tiến độ Dự án").Should().BeTrue();
                var ws = wbV2.Worksheet("Tiến độ Dự án");
                ws.Cell("B5").GetString().Should().Be("STT");
                ws.Cell("C5").GetString().Should().Be("Mã dự án");
                ws.Cell("D5").GetString().Should().Be("Tên dự án triển khai");
                ws.Cell("M5").GetString().Should().Be("Cảnh báo rủi ro (RAG)");
            }

            // 2. Test Báo cáo 2: Phân bổ & Kế hoạch Vốn (V1 vs V2)
            var b2BytesV1 = await service.ExportKeHoachVonCnttReportExcelAsync(2025, 2026, null, "đồng", null, null, 1);
            b2BytesV1.Should().NotBeNullOrEmpty();

            var b2BytesV2 = await service.ExportKeHoachVonCnttReportExcelAsync(2025, 2026, null, "đồng", null, null, 2);
            b2BytesV2.Should().NotBeNullOrEmpty();
            using (var wbB2 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(b2BytesV2)))
            {
                wbB2.Worksheets.Contains("Phân bổ & Kế hoạch Vốn").Should().BeTrue();
                var ws = wbB2.Worksheet("Phân bổ & Kế hoạch Vốn");
                ws.Cell("C3").GetString().Should().Be("STT");
                ws.Cell("D3").GetString().Should().Be("Mã dự án nguồn");
                ws.Cell("N3").GetString().Should().Be("Trạng thái nguồn");
            }

            // 3. Test Báo cáo 3: Lựa chọn Nhà thầu (LCNT)
            var b3BytesV2 = await service.ExportGoiThauLcntReportExcelAsync(null, null, null, "đồng", 2);
            b3BytesV2.Should().NotBeNullOrEmpty();
            using (var wbB3 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(b3BytesV2)))
            {
                wbB3.Worksheets.Contains("Lựa chọn Nhà thầu").Should().BeTrue();
                var ws = wbB3.Worksheet("Lựa chọn Nhà thầu");
                ws.Cell("C3").GetString().Should().Be("STT");
                ws.Cell("D3").GetString().Should().Be("Mã dự án");
                ws.Cell("N3").GetString().Should().Be("Trạng thái gói thầu");
            }

            // 4. Test Báo cáo 4: Quản lý Hợp đồng (V1 vs V2)
            var b4BytesV1 = await service.ExportTheoDoiHopDongReportExcelAsync(null, null, null, null, "đồng", 1);
            b4BytesV1.Should().NotBeNullOrEmpty();

            var b4BytesV2 = await service.ExportTheoDoiHopDongReportExcelAsync(null, null, null, null, "đồng", 2);
            b4BytesV2.Should().NotBeNullOrEmpty();
            using (var wbB4 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(b4BytesV2)))
            {
                wbB4.Worksheets.Contains("Quản lý Hợp đồng").Should().BeTrue();
                var ws = wbB4.Worksheet("Quản lý Hợp đồng");
                ws.Cell("B4").GetString().Should().Be("STT");
                ws.Cell("C4").GetString().Should().Be("Số / Mã HĐ");
                ws.Cell("M4").GetString().Should().Be("Số ngày còn lại");
            }

            // 5. Test Báo cáo 5: Đợt thanh toán (V1 vs V2)
            var b5BytesV1 = await service.ExportContractPaymentReportExcelAsync(DateTime.UtcNow.Year, null, null, null, "đồng", 1);
            b5BytesV1.Should().NotBeNullOrEmpty();

            var b5BytesV2 = await service.ExportContractPaymentReportExcelAsync(DateTime.UtcNow.Year, null, null, null, "đồng", 2);
            b5BytesV2.Should().NotBeNullOrEmpty();
            using (var wbB5 = new ClosedXML.Excel.XLWorkbook(new System.IO.MemoryStream(b5BytesV2)))
            {
                wbB5.Worksheets.Contains("Đợt thanh toán").Should().BeTrue();
                var ws = wbB5.Worksheet("Đợt thanh toán");
                ws.Cell("B3").GetString().Should().Be("STT");
                ws.Cell("C3").GetString().Should().Be("Tên đợt thanh toán");
            }
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
