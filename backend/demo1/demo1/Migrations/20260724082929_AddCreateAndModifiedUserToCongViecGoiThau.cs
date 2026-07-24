using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddCreateAndModifiedUserToCongViecGoiThau : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreateUserId",
                table: "CongViecGoiThaus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedUserId",
                table: "CongViecGoiThaus",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_CreateUserId",
                table: "CongViecGoiThaus",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_ModifiedUserId",
                table: "CongViecGoiThaus",
                column: "ModifiedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CongViecGoiThaus_Users_CreateUserId",
                table: "CongViecGoiThaus",
                column: "CreateUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CongViecGoiThaus_Users_ModifiedUserId",
                table: "CongViecGoiThaus",
                column: "ModifiedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CongViecGoiThaus_Users_CreateUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropForeignKey(
                name: "FK_CongViecGoiThaus_Users_ModifiedUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_CongViecGoiThaus_CreateUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_CongViecGoiThaus_ModifiedUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "CreateUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "ModifiedUserId",
                table: "CongViecGoiThaus");
        }
    }
}
