using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultPhanLoaiDuAns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "PhanLoaiDuAns",
                columns: new[] { "Id", "Code", "Name", "Description", "IsActive", "CreatedAt", "UpdatedAt", "IsDeleted", "DeletedAt", "DeletedByUserId" },
                values: new object[,]
                {
                    { new Guid("019183bf-8c22-7492-b222-000000000001"), "PL_CNTT", "Dự án Công nghệ thông tin", "Dự án đầu tư hạ tầng, phần mềm và giải pháp CNTT", true, now, null, false, null, null },
                    { new Guid("019183bf-8c22-7492-b222-000000000002"), "PL_XDCB", "Dự án Xây dựng cơ bản & Bảo trì", "Sửa chữa, cải tạo trụ sở, phòng giao dịch", true, now, null, false, null, null },
                    { new Guid("019183bf-8c22-7492-b222-000000000003"), "PL_MSHH_DV", "Mua sắm hàng hóa & Dịch vụ", "Trang thiết bị văn phòng, dịch vụ tư vấn...", true, now, null, false, null, null },
                    { new Guid("019183bf-8c22-7492-b222-000000000004"), "PL_DIGITAL_BANKING", "Dự án Ngân hàng số & Thẻ", "Core Banking, eBiz, Chatbot, Thẻ...", true, now, null, false, null, null },
                    { new Guid("019183bf-8c22-7492-b222-000000000005"), "PL_SECURITY", "Dự án An toàn thông tin & Bảo mật", "Bảo mật mạng, SOC, An ninh thông tin...", true, now, null, false, null, null },
                    { new Guid("019183bf-8c22-7492-b222-000000000006"), "PL_KHAC", "Dự án / Phân loại khác", "Các loại dự án khác", true, now, null, false, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000001"));

            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000002"));

            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000003"));

            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000004"));

            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000005"));

            migrationBuilder.DeleteData(
                table: "PhanLoaiDuAns",
                keyColumn: "Id",
                keyValue: new Guid("019183bf-8c22-7492-b222-000000000006"));
        }
    }
}
