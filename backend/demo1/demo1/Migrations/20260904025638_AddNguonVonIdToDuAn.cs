using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNguonVonIdToDuAn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NguonVonId",
                table: "DuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_NguonVonId",
                table: "DuAns",
                column: "NguonVonId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuAns_NguonVons_NguonVonId",
                table: "DuAns",
                column: "NguonVonId",
                principalTable: "NguonVons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuAns_NguonVons_NguonVonId",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_NguonVonId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "NguonVonId",
                table: "DuAns");
        }
    }
}
