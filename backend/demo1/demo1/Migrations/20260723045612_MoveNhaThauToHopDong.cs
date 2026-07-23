using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class MoveNhaThauToHopDong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NhaThauGoiThaus_GoiThaus_GoiThauId",
                table: "NhaThauGoiThaus");

            migrationBuilder.RenameColumn(
                name: "GoiThauId",
                table: "NhaThauGoiThaus",
                newName: "HopDongId");

            migrationBuilder.RenameIndex(
                name: "IX_NhaThauGoiThaus_GoiThauId",
                table: "NhaThauGoiThaus",
                newName: "IX_NhaThauGoiThaus_HopDongId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDaiDienLienDanh",
                table: "NhaThauGoiThaus",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddForeignKey(
                name: "FK_NhaThauGoiThaus_HopDongs_HopDongId",
                table: "NhaThauGoiThaus",
                column: "HopDongId",
                principalTable: "HopDongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NhaThauGoiThaus_HopDongs_HopDongId",
                table: "NhaThauGoiThaus");

            migrationBuilder.RenameColumn(
                name: "HopDongId",
                table: "NhaThauGoiThaus",
                newName: "GoiThauId");

            migrationBuilder.RenameIndex(
                name: "IX_NhaThauGoiThaus_HopDongId",
                table: "NhaThauGoiThaus",
                newName: "IX_NhaThauGoiThaus_GoiThauId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDaiDienLienDanh",
                table: "NhaThauGoiThaus",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NhaThauGoiThaus_GoiThaus_GoiThauId",
                table: "NhaThauGoiThaus",
                column: "GoiThauId",
                principalTable: "GoiThaus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
