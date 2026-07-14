using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNhaThauGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhaThauGoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GoiThauId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NhaThauId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsLienDanh = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TenLienDanh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDaiDienLienDanh = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TyLeLienDanh = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GiaTriDamNhan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VaiTroTrongLienDanh = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_NhaThauGoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhaThauGoiThaus_DoiTacs_NhaThauId",
                        column: x => x.NhaThauId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhaThauGoiThaus_GoiThaus_GoiThauId",
                        column: x => x.GoiThauId,
                        principalTable: "GoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_Code",
                table: "NhaThauGoiThaus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_GoiThauId",
                table: "NhaThauGoiThaus",
                column: "GoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_NhaThauId",
                table: "NhaThauGoiThaus",
                column: "NhaThauId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhaThauGoiThaus");
        }
    }
}
