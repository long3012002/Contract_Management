using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddHinhThucLcntToGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HinhThucLcnt",
                table: "GoiThaus",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongThucLcnt",
                table: "GoiThaus",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HinhThucLcnt",
                table: "GoiThaus");

            migrationBuilder.DropColumn(
                name: "PhuongThucLcnt",
                table: "GoiThaus");
        }
    }
}
