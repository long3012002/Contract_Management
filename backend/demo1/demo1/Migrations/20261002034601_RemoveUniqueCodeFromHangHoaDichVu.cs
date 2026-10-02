using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueCodeFromHangHoaDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS public.\"IX_HangHoaDichVus_Code\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS public.\"IX_HangHoaDichVus_Loai_Code\";");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "HangHoaDichVus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "HangHoaDichVus",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_Loai_Code",
                table: "HangHoaDichVus",
                columns: new[] { "Loai", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
