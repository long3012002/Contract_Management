using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNguonVonTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NguonVons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguonVons", x => x.Id);
                });

            var now = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "NguonVons",
                columns: new[] { "Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedByUserId" },
                values: new object[,]
                {
                    { new Guid("019183bf-7b11-7391-a111-000000000001"), "NV_NHHT", "Chi phí của NHHT", null, true, now, null, false, null, null },
                    { new Guid("019183bf-7b11-7391-a111-000000000002"), "NV_CN", "Chi phí tại chi nhánh", null, true, now, null, false, null, null },
                    { new Guid("019183bf-7b11-7391-a111-000000000003"), "NV_KHAC", "Nguồn khác", null, true, now, null, false, null, null },
                    { new Guid("019183bf-7b11-7391-a111-000000000004"), "NV_QPL", "Quỹ phúc lợi", null, true, now, null, false, null, null },
                    { new Guid("019183bf-7b11-7391-a111-000000000005"), "NV_QDTPT", "Quỹ đầu tư phát triển", null, true, now, null, false, null, null },
                    { new Guid("019183bf-7b11-7391-a111-000000000006"), "NV_VDL_QDTR", "Vốn điều lệ và Quỹ dự trữ bổ sung vốn điều lệ", null, true, now, null, false, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NguonVons");
        }
    }
}
