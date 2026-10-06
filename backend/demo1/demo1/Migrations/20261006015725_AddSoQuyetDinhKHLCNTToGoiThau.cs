using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddSoQuyetDinhKHLCNTToGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HopDongs_DuAnId",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_DotThanhToans_HopDongId",
                table: "DotThanhToans");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayPheDuyetKHLCNT",
                table: "GoiThaus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoQuyetDinhKHLCNT",
                table: "GoiThaus",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_NgayKetThuc_TrangThai",
                table: "Licenses",
                columns: new[] { "NgayKetThuc", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_DuAnId_IsActive_IsDeleted",
                table: "HopDongs",
                columns: new[] { "DuAnId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_DotThanhToans_HopDongId_IsPaid",
                table: "DotThanhToans",
                columns: new[] { "HopDongId", "IsPaid" });

            migrationBuilder.CreateIndex(
                name: "IX_DotThanhToans_NgayThanhToanThucTe",
                table: "DotThanhToans",
                column: "NgayThanhToanThucTe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Licenses_NgayKetThuc_TrangThai",
                table: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_DuAnId_IsActive_IsDeleted",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_DotThanhToans_HopDongId_IsPaid",
                table: "DotThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_DotThanhToans_NgayThanhToanThucTe",
                table: "DotThanhToans");

            migrationBuilder.DropColumn(
                name: "NgayPheDuyetKHLCNT",
                table: "GoiThaus");

            migrationBuilder.DropColumn(
                name: "SoQuyetDinhKHLCNT",
                table: "GoiThaus");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_DuAnId",
                table: "HopDongs",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DotThanhToans_HopDongId",
                table: "DotThanhToans",
                column: "HopDongId");
        }
    }
}
