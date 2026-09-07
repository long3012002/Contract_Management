using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDuAnNguonTrienKhaiSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuAnNguonTrienKhais_DuAns_NguonProjectId",
                table: "DuAnNguonTrienKhais");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DuAnNguonTrienKhais",
                table: "DuAnNguonTrienKhais");

            migrationBuilder.DropIndex(
                name: "IX_DuAnNguonTrienKhais_NguonProjectId",
                table: "DuAnNguonTrienKhais");

            migrationBuilder.Sql(@"
                CREATE TEMP TABLE temp_du_an_nguon AS
                SELECT ""TrienKhaiProjectId"", string_agg(""NguonProjectId""::text, ';') AS ""NguonProjectId"", MIN(""CreatedAt"") AS ""CreatedAt""
                FROM ""DuAnNguonTrienKhais""
                GROUP BY ""TrienKhaiProjectId"";

                DELETE FROM ""DuAnNguonTrienKhais"";
            ");

            migrationBuilder.AlterColumn<string>(
                name: "NguonProjectId",
                table: "DuAnNguonTrienKhais",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.Sql(@"
                INSERT INTO ""DuAnNguonTrienKhais"" (""TrienKhaiProjectId"", ""NguonProjectId"", ""CreatedAt"")
                SELECT ""TrienKhaiProjectId"", ""NguonProjectId"", ""CreatedAt""
                FROM temp_du_an_nguon;

                DROP TABLE temp_du_an_nguon;
            ");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DuAnNguonTrienKhais",
                table: "DuAnNguonTrienKhais",
                column: "TrienKhaiProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonTrienKhais_TrienKhaiProjectId",
                table: "DuAnNguonTrienKhais",
                column: "TrienKhaiProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DuAnNguonTrienKhais",
                table: "DuAnNguonTrienKhais");

            migrationBuilder.DropIndex(
                name: "IX_DuAnNguonTrienKhais_TrienKhaiProjectId",
                table: "DuAnNguonTrienKhais");

            migrationBuilder.AlterColumn<Guid>(
                name: "NguonProjectId",
                table: "DuAnNguonTrienKhais",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DuAnNguonTrienKhais",
                table: "DuAnNguonTrienKhais",
                columns: new[] { "TrienKhaiProjectId", "NguonProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonTrienKhais_NguonProjectId",
                table: "DuAnNguonTrienKhais",
                column: "NguonProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuAnNguonTrienKhais_DuAns_NguonProjectId",
                table: "DuAnNguonTrienKhais",
                column: "NguonProjectId",
                principalTable: "DuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
