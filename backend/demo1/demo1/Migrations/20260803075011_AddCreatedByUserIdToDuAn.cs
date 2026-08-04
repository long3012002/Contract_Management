using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToDuAn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "DuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_CreatedByUserId",
                table: "DuAns",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DuAns_Users_CreatedByUserId",
                table: "DuAns",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DuAns_Users_CreatedByUserId",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_CreatedByUserId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "DuAns");
        }
    }
}
