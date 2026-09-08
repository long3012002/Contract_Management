using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class PendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PhuLucHopDongId",
                table: "HangHoaDichVus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PhuLucHopDongId",
                table: "DotThanhToans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhuLucHopDongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HopDongId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoPhuLuc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenPhuLuc = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LoaiPhuLuc = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    NgayKy = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NgayHieuLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GiaTriDieuChinh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpiredDateMoi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThoiHanThucHienMoi = table.Column<string>(type: "text", nullable: true),
                    NoiDungDieuChinh = table.Column<string>(type: "text", nullable: true),
                    GhiChu = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuLucHopDongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhuLucHopDongs_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_XuatXu_CreatedAt_Id_Desc",
                table: "XuatXus",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Resolution_CreatedAt_Id_Desc",
                table: "Resolutions",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PhanLoaiDuAn_CreatedAt_Id_Desc",
                table: "PhanLoaiDuAns",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NhomDuAn_CreatedAt_Id_Desc",
                table: "NhomDuAns",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiHopDong_CreatedAt_Id_Desc",
                table: "LoaiHopDongs",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_License_CreatedAt_Id_Desc",
                table: "Licenses",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_CreatedAt_Id_Desc",
                table: "HopDongs",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HangSanXuat_CreatedAt_Id_Desc",
                table: "HangSanXuats",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVu_CreatedAt_Id_Desc",
                table: "HangHoaDichVus",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_PhuLucHopDongId",
                table: "HangHoaDichVus",
                column: "PhuLucHopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_GoiThau_CreatedAt_Id_Desc",
                table: "GoiThaus",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DuAn_CreatedAt_Id_Desc",
                table: "DuAns",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DotThanhToans_PhuLucHopDongId",
                table: "DotThanhToans",
                column: "PhuLucHopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_DonViTinh_CreatedAt_Id_Desc",
                table: "DonViTinhs",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DoiTac_CreatedAt_Id_Desc",
                table: "DoiTacs",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAn_CreatedAt_Id_Desc",
                table: "DieuChinhDuAns",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTiep_CreatedAt_Id_Desc",
                table: "CongViecLichSuChuyenTieps",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThau_CreatedAt_Id_Desc",
                table: "CongViecGoiThaus",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PhuLucHopDong_CreatedAt_Id_Desc",
                table: "PhuLucHopDongs",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PhuLucHopDongs_Code",
                table: "PhuLucHopDongs",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PhuLucHopDongs_HopDongId",
                table: "PhuLucHopDongs",
                column: "HopDongId");

            migrationBuilder.AddForeignKey(
                name: "FK_DotThanhToans_PhuLucHopDongs_PhuLucHopDongId",
                table: "DotThanhToans",
                column: "PhuLucHopDongId",
                principalTable: "PhuLucHopDongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HangHoaDichVus_PhuLucHopDongs_PhuLucHopDongId",
                table: "HangHoaDichVus",
                column: "PhuLucHopDongId",
                principalTable: "PhuLucHopDongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DotThanhToans_PhuLucHopDongs_PhuLucHopDongId",
                table: "DotThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_HangHoaDichVus_PhuLucHopDongs_PhuLucHopDongId",
                table: "HangHoaDichVus");

            migrationBuilder.DropTable(
                name: "PhuLucHopDongs");

            migrationBuilder.DropIndex(
                name: "IX_XuatXu_CreatedAt_Id_Desc",
                table: "XuatXus");

            migrationBuilder.DropIndex(
                name: "IX_Resolution_CreatedAt_Id_Desc",
                table: "Resolutions");

            migrationBuilder.DropIndex(
                name: "IX_PhanLoaiDuAn_CreatedAt_Id_Desc",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropIndex(
                name: "IX_NhomDuAn_CreatedAt_Id_Desc",
                table: "NhomDuAns");

            migrationBuilder.DropIndex(
                name: "IX_LoaiHopDong_CreatedAt_Id_Desc",
                table: "LoaiHopDongs");

            migrationBuilder.DropIndex(
                name: "IX_License_CreatedAt_Id_Desc",
                table: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_HopDong_CreatedAt_Id_Desc",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HangSanXuat_CreatedAt_Id_Desc",
                table: "HangSanXuats");

            migrationBuilder.DropIndex(
                name: "IX_HangHoaDichVu_CreatedAt_Id_Desc",
                table: "HangHoaDichVus");

            migrationBuilder.DropIndex(
                name: "IX_HangHoaDichVus_PhuLucHopDongId",
                table: "HangHoaDichVus");

            migrationBuilder.DropIndex(
                name: "IX_GoiThau_CreatedAt_Id_Desc",
                table: "GoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_DuAn_CreatedAt_Id_Desc",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DotThanhToans_PhuLucHopDongId",
                table: "DotThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_DonViTinh_CreatedAt_Id_Desc",
                table: "DonViTinhs");

            migrationBuilder.DropIndex(
                name: "IX_DoiTac_CreatedAt_Id_Desc",
                table: "DoiTacs");

            migrationBuilder.DropIndex(
                name: "IX_DieuChinhDuAn_CreatedAt_Id_Desc",
                table: "DieuChinhDuAns");

            migrationBuilder.DropIndex(
                name: "IX_CongViecLichSuChuyenTiep_CreatedAt_Id_Desc",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropIndex(
                name: "IX_CongViecGoiThau_CreatedAt_Id_Desc",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "PhuLucHopDongId",
                table: "HangHoaDichVus");

            migrationBuilder.DropColumn(
                name: "PhuLucHopDongId",
                table: "DotThanhToans");
        }
    }
}
