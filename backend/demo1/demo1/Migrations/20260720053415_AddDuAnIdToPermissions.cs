using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddDuAnIdToPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DuAnId",
                table: "UserPermissions",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "DuAnId",
                table: "PermissionRequests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "PermissionId",
                table: "PermissionRequests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_DuAnId",
                table: "UserPermissions",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_DuAnId",
                table: "PermissionRequests",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_PermissionId",
                table: "PermissionRequests",
                column: "PermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionRequests_DuAns_DuAnId",
                table: "PermissionRequests",
                column: "DuAnId",
                principalTable: "DuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionRequests_UserPermissions_PermissionId",
                table: "PermissionRequests",
                column: "PermissionId",
                principalTable: "UserPermissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_DuAns_DuAnId",
                table: "UserPermissions",
                column: "DuAnId",
                principalTable: "DuAns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_DuAns_DuAnId",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_UserPermissions_PermissionId",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_DuAns_DuAnId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_DuAnId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_DuAnId",
                table: "PermissionRequests");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_PermissionId",
                table: "PermissionRequests");

            migrationBuilder.DropColumn(
                name: "DuAnId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "DuAnId",
                table: "PermissionRequests");

            migrationBuilder.DropColumn(
                name: "PermissionId",
                table: "PermissionRequests");
        }
    }
}
