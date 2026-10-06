using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddQuyetDinhThanhLapToDuAn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayQuyetDinhThanhLap",
                table: "DuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoQuyetDinhThanhLap",
                table: "DuAns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayQuyetDinhThanhLap",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "SoQuyetDinhThanhLap",
                table: "DuAns");
        }
    }
}
