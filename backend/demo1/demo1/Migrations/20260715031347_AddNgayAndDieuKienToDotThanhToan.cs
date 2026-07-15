using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNgayAndDieuKienToDotThanhToan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DieuKienThanhToan",
                table: "DotThanhToans",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayThanhToan",
                table: "DotThanhToans",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DieuKienThanhToan",
                table: "DotThanhToans");

            migrationBuilder.DropColumn(
                name: "NgayThanhToan",
                table: "DotThanhToans");
        }
    }
}
