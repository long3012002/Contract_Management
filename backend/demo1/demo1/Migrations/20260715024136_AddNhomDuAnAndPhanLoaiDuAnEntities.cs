using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNhomDuAnAndPhanLoaiDuAnEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhanLoaiDuAn",
                table: "DuAns");

            migrationBuilder.AddColumn<Guid>(
                name: "NhomDuAnId",
                table: "DuAns",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "PhanLoaiDuAnId",
                table: "DuAns",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "NhomDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomDuAns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PhanLoaiDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanLoaiDuAns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_NhomDuAnId",
                table: "DuAns",
                column: "NhomDuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_PhanLoaiDuAnId",
                table: "DuAns",
                column: "PhanLoaiDuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DuAns_NhomDuAns_NhomDuAnId",
                table: "DuAns",
                column: "NhomDuAnId",
                principalTable: "NhomDuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DuAns_PhanLoaiDuAns_PhanLoaiDuAnId",
                table: "DuAns",
                column: "PhanLoaiDuAnId",
                principalTable: "PhanLoaiDuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuAns_NhomDuAns_NhomDuAnId",
                table: "DuAns");

            migrationBuilder.DropForeignKey(
                name: "FK_DuAns_PhanLoaiDuAns_PhanLoaiDuAnId",
                table: "DuAns");

            migrationBuilder.DropTable(
                name: "NhomDuAns");

            migrationBuilder.DropTable(
                name: "PhanLoaiDuAns");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_NhomDuAnId",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_PhanLoaiDuAnId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "NhomDuAnId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "PhanLoaiDuAnId",
                table: "DuAns");

            migrationBuilder.AddColumn<int>(
                name: "PhanLoaiDuAn",
                table: "DuAns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
