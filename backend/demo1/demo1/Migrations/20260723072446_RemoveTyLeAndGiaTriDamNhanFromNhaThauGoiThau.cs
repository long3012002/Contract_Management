using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTyLeAndGiaTriDamNhanFromNhaThauGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiaTriDamNhan",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "TyLeLienDanh",
                table: "NhaThauGoiThaus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GiaTriDamNhan",
                table: "NhaThauGoiThaus",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TyLeLienDanh",
                table: "NhaThauGoiThaus",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
