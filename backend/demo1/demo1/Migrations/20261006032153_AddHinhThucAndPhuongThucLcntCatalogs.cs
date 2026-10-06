using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddHinhThucAndPhuongThucLcntCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HinhThucLcnts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_HinhThucLcnts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhuongThucLcnts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PhuongThucLcnts", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HinhThucLcnts",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "DeletedByUserId", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-000000000001"), "HT_DTRR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Đấu thầu rộng rãi", true, false, "Đấu thầu rộng rãi", null },
                    { new Guid("11111111-1111-1111-1111-000000000002"), "HT_DTHC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Đấu thầu hạn chế", true, false, "Đấu thầu hạn chế", null },
                    { new Guid("11111111-1111-1111-1111-000000000003"), "HT_CDT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Chỉ định thầu", true, false, "Chỉ định thầu", null },
                    { new Guid("11111111-1111-1111-1111-000000000004"), "HT_CHCT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Chào hàng cạnh tranh", true, false, "Chào hàng cạnh tranh", null },
                    { new Guid("11111111-1111-1111-1111-000000000005"), "HT_MSTT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Mua sắm trực tiếp", true, false, "Mua sắm trực tiếp", null },
                    { new Guid("11111111-1111-1111-1111-000000000006"), "HT_TTH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Tự thực hiện", true, false, "Tự thực hiện", null },
                    { new Guid("11111111-1111-1111-1111-000000000007"), "HT_TGTHCD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Tham gia thực hiện của cộng đồng", true, false, "Tham gia thực hiện của cộng đồng", null },
                    { new Guid("11111111-1111-1111-1111-000000000008"), "HT_DPG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Đàm phán giá", true, false, "Đàm phán giá", null },
                    { new Guid("11111111-1111-1111-1111-000000000009"), "HT_DH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Đặt hàng", true, false, "Đặt hàng", null },
                    { new Guid("11111111-1111-1111-1111-000000000010"), "HT_THDB", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Lựa chọn nhà thầu trong trường hợp đặc biệt", true, false, "Lựa chọn nhà thầu trong trường hợp đặc biệt", null }
                });

            migrationBuilder.InsertData(
                table: "PhuongThucLcnts",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "DeletedByUserId", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-000000000001"), "PT_1G1T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Phương thức một giai đoạn một túi hồ sơ", true, false, "Phương thức một giai đoạn một túi hồ sơ", null },
                    { new Guid("22222222-2222-2222-2222-000000000002"), "PT_1G2T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Phương thức một giai đoạn hai túi hồ sơ", true, false, "Phương thức một giai đoạn hai túi hồ sơ", null },
                    { new Guid("22222222-2222-2222-2222-000000000003"), "PT_2G1T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Phương thức hai giai đoạn một túi hồ sơ", true, false, "Phương thức hai giai đoạn một túi hồ sơ", null },
                    { new Guid("22222222-2222-2222-2222-000000000004"), "PT_2G2T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Phương thức hai giai đoạn hai túi hồ sơ", true, false, "Phương thức hai giai đoạn hai túi hồ sơ", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HinhThucLcnt_CreatedAt_Id_Desc",
                table: "HinhThucLcnts",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HinhThucLcnts_Code",
                table: "HinhThucLcnts",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PhuongThucLcnt_CreatedAt_Id_Desc",
                table: "PhuongThucLcnts",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_PhuongThucLcnts_Code",
                table: "PhuongThucLcnts",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HinhThucLcnts");

            migrationBuilder.DropTable(
                name: "PhuongThucLcnts");
        }
    }
}
