using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueCodeIndexFromKeHoachVonAndDuAnGopLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KeHoachVons_Code",
                table: "KeHoachVons");

            migrationBuilder.DropIndex(
                name: "IX_DuAnGopLinks_Code",
                table: "DuAnGopLinks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVons_Code",
                table: "KeHoachVons",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLinks_Code",
                table: "DuAnGopLinks",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
