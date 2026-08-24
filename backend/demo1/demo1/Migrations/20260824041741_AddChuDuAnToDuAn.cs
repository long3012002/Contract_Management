using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddChuDuAnToDuAn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChuDuAnId",
                table: "DuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_ChuDuAnId",
                table: "DuAns",
                column: "ChuDuAnId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuAns_Users_ChuDuAnId",
                table: "DuAns",
                column: "ChuDuAnId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuAns_Users_ChuDuAnId",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_ChuDuAnId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "ChuDuAnId",
                table: "DuAns");
        }
    }
}
