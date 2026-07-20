using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DuAnId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    HopDongId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    NhaCungCapId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    LoaiLicense = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: true),
                    ThongTinThietBi = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayBatDau = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CanhBaoTruocNgay = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
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
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenses_DoiTacs_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Licenses_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenses_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 27, 45, 201, DateTimeKind.Utc).AddTicks(9350));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 27, 45, 202, DateTimeKind.Utc).AddTicks(462));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 27, 45, 202, DateTimeKind.Utc).AddTicks(485));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 27, 45, 202, DateTimeKind.Utc).AddTicks(490));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 27, 45, 202, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Code",
                table: "Licenses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_DuAnId",
                table: "Licenses",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_HopDongId",
                table: "Licenses",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_NhaCungCapId",
                table: "Licenses",
                column: "NhaCungCapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 1, 16, 768, DateTimeKind.Utc).AddTicks(5216));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 1, 16, 768, DateTimeKind.Utc).AddTicks(5976));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 1, 16, 768, DateTimeKind.Utc).AddTicks(5981));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 1, 16, 768, DateTimeKind.Utc).AddTicks(5984));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "CreatedAt",
                value: new DateTime(2026, 7, 20, 7, 1, 16, 768, DateTimeKind.Utc).AddTicks(5985));
        }
    }
}
