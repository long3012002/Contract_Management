using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddDuAnNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HinhThucQuanLy",
                table: "DuAns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoiDung",
                table: "DuAns",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ToChucThucHien",
                table: "DuAns",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HinhThucQuanLy",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "NoiDung",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "ToChucThucHien",
                table: "DuAns");
        }
    }
}
