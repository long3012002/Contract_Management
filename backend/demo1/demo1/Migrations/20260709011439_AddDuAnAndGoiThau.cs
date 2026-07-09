using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddDuAnAndGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_BidPackages_BidPackageId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_Projects_ProjectId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "BidPackages");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "Contracts",
                newName: "GoiThauId");

            migrationBuilder.RenameColumn(
                name: "BidPackageId",
                table: "Contracts",
                newName: "DuAnId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_ProjectId",
                table: "Contracts",
                newName: "IX_Contracts_GoiThauId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_BidPackageId",
                table: "Contracts",
                newName: "IX_Contracts_DuAnId");

            migrationBuilder.CreateTable(
                name: "DuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DuToanPheDuyet = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoaiDuAn = table.Column<int>(type: "int", nullable: false),
                    NguonDuAnIds = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChuDauTu = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiaDiemThucHien = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianThucHien = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayBatDau = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NamBatDau = table.Column<int>(type: "int", nullable: true),
                    NamKetThuc = table.Column<int>(type: "int", nullable: true),
                    DaKetThuc = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_DuAns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DieuChinhDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DuAnId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GiaTriDieuChinh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LyDoDieuChinh = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayDieuChinh = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_DieuChinhDuAns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DieuChinhDuAns_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DuAnId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    GiaTriGoiThau = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NguongCanhBaoPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_GoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoiThaus_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_DuAnId",
                table: "DieuChinhDuAns",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_Code",
                table: "DuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_DuAnId",
                table: "GoiThaus",
                column: "DuAnId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_DuAns_DuAnId",
                table: "Contracts",
                column: "DuAnId",
                principalTable: "DuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_GoiThaus_GoiThauId",
                table: "Contracts",
                column: "GoiThauId",
                principalTable: "GoiThaus",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_DuAns_DuAnId",
                table: "Contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_Contracts_GoiThaus_GoiThauId",
                table: "Contracts");

            migrationBuilder.DropTable(
                name: "DieuChinhDuAns");

            migrationBuilder.DropTable(
                name: "GoiThaus");

            migrationBuilder.DropTable(
                name: "DuAns");

            migrationBuilder.RenameColumn(
                name: "GoiThauId",
                table: "Contracts",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "DuAnId",
                table: "Contracts",
                newName: "BidPackageId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_GoiThauId",
                table: "Contracts",
                newName: "IX_Contracts_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Contracts_DuAnId",
                table: "Contracts",
                newName: "IX_Contracts_BidPackageId");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BidPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProjectId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WarningThresholdPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BidPackages_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BidPackages_Code",
                table: "BidPackages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BidPackages_ProjectId",
                table: "BidPackages",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Code",
                table: "Projects",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_BidPackages_BidPackageId",
                table: "Contracts",
                column: "BidPackageId",
                principalTable: "BidPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Projects_ProjectId",
                table: "Contracts",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
