using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBaseEntityFromNhaThauGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NhaThauGoiThaus_Code",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "NhaThauGoiThaus");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "NhaThauGoiThaus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "NhaThauGoiThaus",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "NhaThauGoiThaus",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "NhaThauGoiThaus",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "NhaThauGoiThaus",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_Code",
                table: "NhaThauGoiThaus",
                column: "Code",
                unique: true);
        }
    }
}
