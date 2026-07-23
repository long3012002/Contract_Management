using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLienDanhDetailsFromNhaThauGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDaiDienLienDanh",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "TenLienDanh",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "VaiTroTrongLienDanh",
                table: "NhaThauGoiThaus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDaiDienLienDanh",
                table: "NhaThauGoiThaus",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenLienDanh",
                table: "NhaThauGoiThaus",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VaiTroTrongLienDanh",
                table: "NhaThauGoiThaus",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
