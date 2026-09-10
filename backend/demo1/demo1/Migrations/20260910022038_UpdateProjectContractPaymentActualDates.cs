using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectContractPaymentActualDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaKetThuc",
                table: "HopDongs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayKetThucThucTe",
                table: "HopDongs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayKetThucThucTe",
                table: "DuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhiChuThanhToan",
                table: "DotThanhToans",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayThanhToanThucTe",
                table: "DotThanhToans",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaKetThuc",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "NgayKetThucThucTe",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "NgayKetThucThucTe",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "GhiChuThanhToan",
                table: "DotThanhToans");

            migrationBuilder.DropColumn(
                name: "NgayThanhToanThucTe",
                table: "DotThanhToans");
        }
    }
}
