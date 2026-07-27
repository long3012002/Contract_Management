using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddLoaiHanhDongToCongViecLichSuChuyenTiep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoaiHanhDong",
                table: "CongViecLichSuChuyenTieps",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoaiHanhDong",
                table: "CongViecLichSuChuyenTieps");
        }
    }
}
