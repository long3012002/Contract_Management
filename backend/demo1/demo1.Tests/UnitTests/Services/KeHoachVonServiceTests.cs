using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using demo1.Data;
using demo1.DTOs;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using demo1.Services.Implements;
using demo1.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace demo1.Tests.UnitTests.Services
{
    public class KeHoachVonServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly KeHoachVonService _service;

        public KeHoachVonServiceTests()
        {
            _dbContext = DbContextTestHelper.CreateSqliteInMemoryDbContext();
            _service = new KeHoachVonService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Fact]
        public async Task KeHoachVonService_GetByIdAsync_Should_Include_NguonVonChiTiet_And_TongTheoNguonVon()
        {
            // Arrange
            var nv1 = new NguonVon { Id = Guid.NewGuid(), Code = "NV_DAUTU", Name = "Nguồn Đầu Tư" };
            var nv2 = new NguonVon { Id = Guid.NewGuid(), Code = "NV_KHAC", Name = "Nguồn Khác" };
            _dbContext.NguonVons.AddRange(nv1, nv2);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-01", Name = "Dự án A" };
            project.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nv1.Id, SoTien = 1000000000m, Nam = 2026 });
            project.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nv2.Id, SoTien = 500000000m, Nam = 2026 });
            _dbContext.DuAns.Add(project);

            var khv = new KeHoachVon
            {
                Id = Guid.NewGuid(),
                NamKeHoach = 2026,
                LoaiKeHoach = 1,
                TrangThai = 3,
                TongMucDeNghi = 1500000000m,
                TongMucDuocDuyet = 1500000000m
            };
            khv.KeHoachVonDuAns.Add(new KeHoachVonDuAn
            {
                KeHoachVonId = khv.Id,
                DuAnId = project.Id,
                SoTienDeNghi = 1500000000m,
                SoTienDuocDuyet = 1500000000m
            });
            _dbContext.KeHoachVons.Add(khv);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(khv.Id);

            // Assert
            result.Should().NotBeNull();
            result!.DanhSachDuAn.Should().HaveCount(1);
            var projectItem = result.DanhSachDuAn[0];
            projectItem.NguonVonChiTiet.Should().HaveCount(2);
            projectItem.NguonVonChiTiet.First(x => x.NguonVonId == nv1.Id).SoTien.Should().Be(1000000000m);
            projectItem.NguonVonChiTiet.First(x => x.NguonVonId == nv2.Id).SoTien.Should().Be(500000000m);

            result.TongTheoNguonVon.Should().HaveCount(2);
            result.TongTheoNguonVon.First(x => x.NguonVonId == nv1.Id).TongSoTien.Should().Be(1000000000m);
            result.TongTheoNguonVon.First(x => x.NguonVonId == nv2.Id).TongSoTien.Should().Be(500000000m);
        }

        [Fact]
        public async Task KeHoachVonService_GetByIdAsync_With_DonViTinh_Trieu_Should_Convert_Amounts()
        {
            // Arrange
            var nv1 = new NguonVon { Id = Guid.NewGuid(), Code = "NV_DAUTU", Name = "Nguồn Đầu Tư" };
            _dbContext.NguonVons.Add(nv1);

            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-02", Name = "Dự án B" };
            project.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nv1.Id, SoTien = 2000000000m, Nam = 2026 });
            _dbContext.DuAns.Add(project);

            var khv = new KeHoachVon
            {
                Id = Guid.NewGuid(),
                NamKeHoach = 2026,
                LoaiKeHoach = 1,
                TrangThai = 3,
                TongMucDeNghi = 2000000000m,
                TongMucDuocDuyet = 2000000000m
            };
            khv.KeHoachVonDuAns.Add(new KeHoachVonDuAn
            {
                KeHoachVonId = khv.Id,
                DuAnId = project.Id,
                SoTienDeNghi = 2000000000m,
                SoTienDuocDuyet = 2000000000m
            });
            _dbContext.KeHoachVons.Add(khv);
            await _dbContext.SaveChangesAsync();

            // Act: donViTinh = "trieu" -> divide by 1,000,000
            var result = await _service.GetByIdAsync(khv.Id, "trieu");

            // Assert
            result.Should().NotBeNull();
            result!.TongMucDeNghi.Should().Be(2000m);
            result.TongMucDuocDuyet.Should().Be(2000m);
            var projectItem = result.DanhSachDuAn[0];
            projectItem.SoTienDeNghi.Should().Be(2000m);
            projectItem.SoTienDuocDuyet.Should().Be(2000m);
            projectItem.NguonVonChiTiet[0].SoTien.Should().Be(2000m);
            result.TongTheoNguonVon[0].TongSoTien.Should().Be(2000m);
        }

        [Fact]
        public async Task KeHoachVonService_GetLichSuKeHoachVonByDuAnIdAsync_Should_Return_History_With_NguonVonChiTiet()
        {
            // Arrange
            var project = new DuAn { Id = Guid.NewGuid(), Code = "DA-03", Name = "Dự án C" };
            var nv = new NguonVon { Id = Guid.NewGuid(), Code = "NV_A", Name = "Nguồn A" };
            _dbContext.NguonVons.Add(nv);
            project.DanhSachNguonVon.Add(new DuAnNguonVon { Id = Guid.NewGuid(), DuAnId = project.Id, NguonVonId = nv.Id, SoTien = 500000000m, Nam = 2026 });
            _dbContext.DuAns.Add(project);

            var khv = new KeHoachVon { Id = Guid.NewGuid(), NamKeHoach = 2026, LoaiKeHoach = 1, TrangThai = 3 };
            khv.KeHoachVonDuAns.Add(new KeHoachVonDuAn { KeHoachVonId = khv.Id, DuAnId = project.Id, SoTienDeNghi = 500000000m, SoTienDuocDuyet = 500000000m });
            _dbContext.KeHoachVons.Add(khv);
            await _dbContext.SaveChangesAsync();

            // Act
            var history = await _service.GetLichSuKeHoachVonByDuAnIdAsync(project.Id, "trieu");

            // Assert
            history.Should().NotBeEmpty();
            history[0].SoTienDeNghi.Should().Be(500m);
            history[0].NguonVonChiTiet.Should().HaveCount(1);
            history[0].NguonVonChiTiet[0].SoTien.Should().Be(500m);
        }

        [Fact]
        public async Task KeHoachVonService_CreateAsync_Should_Create_Multiple_Entries_Without_Duplicate_Code_Error()
        {
            // Arrange
            var dto1 = new CreateKeHoachVonDto { NamKeHoach = 2026, LoaiKeHoach = 1, SoQuyetDinh = "QD-01" };
            var dto2 = new CreateKeHoachVonDto { NamKeHoach = 2026, LoaiKeHoach = 2, SoQuyetDinh = "QD-02" };

            // Act
            var res1 = await _service.CreateAsync(dto1, null);
            var res2 = await _service.CreateAsync(dto2, null);

            // Assert
            res1.Should().NotBeNull();
            res2.Should().NotBeNull();
            var all = _dbContext.KeHoachVons.ToList();
            all.Should().HaveCount(2);
        }
    }
}
