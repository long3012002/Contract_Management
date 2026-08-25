using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class SeedLoaiHopDongBaoTri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "LoaiHopDongs",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "DeletedByUserId", "Description", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[] { new Guid("66666666-6666-6666-6666-666666666666"), "01", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Hợp đồng bảo trì (Mặc định)", true, false, "Bảo trì", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LoaiHopDongs",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));
        }
    }
}
