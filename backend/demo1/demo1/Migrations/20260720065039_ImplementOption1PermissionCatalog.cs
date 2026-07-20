using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class ImplementOption1PermissionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanDelete",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "UserPermissions");

            migrationBuilder.AddColumn<Guid>(
                name: "PermissionId",
                table: "UserPermissions",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestedPermissionId",
                table: "PermissionRequests",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "VIEW", new DateTime(2026, 7, 20, 6, 50, 38, 497, DateTimeKind.Utc).AddTicks(7139), "Quyền xem dữ liệu", "Xem" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "CREATE", new DateTime(2026, 7, 20, 6, 50, 38, 497, DateTimeKind.Utc).AddTicks(7926), "Quyền tạo mới dữ liệu", "Tạo mới" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "EDIT", new DateTime(2026, 7, 20, 6, 50, 38, 497, DateTimeKind.Utc).AddTicks(7932), "Quyền chỉnh sửa bản ghi", "Chỉnh sửa" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "DELETE", new DateTime(2026, 7, 20, 6, 50, 38, 497, DateTimeKind.Utc).AddTicks(7934), "Quyền xóa bản ghi", "Xóa" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "APPROVE", new DateTime(2026, 7, 20, 6, 50, 38, 497, DateTimeKind.Utc).AddTicks(7936), "Quyền phê duyệt yêu cầu", "Phê duyệt" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_User_DuAn_Perm",
                table: "UserPermissions",
                columns: new[] { "UserId", "DuAnId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_User_Entity_Perm",
                table: "UserPermissions",
                columns: new[] { "UserId", "EntityName", "EntityId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedPermissionId",
                table: "PermissionRequests",
                column: "RequestedPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionRequests_Permissions_RequestedPermissionId",
                table: "PermissionRequests",
                column: "RequestedPermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserPermissions_Permissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PermissionRequests_Permissions_RequestedPermissionId",
                table: "PermissionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPermissions_Permissions_PermissionId",
                table: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPermission_User_DuAn_Perm",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPermission_User_Entity_Perm",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions");

            migrationBuilder.DropIndex(
                name: "IX_PermissionRequests_RequestedPermissionId",
                table: "PermissionRequests");

            migrationBuilder.DropColumn(
                name: "PermissionId",
                table: "UserPermissions");

            migrationBuilder.DropColumn(
                name: "RequestedPermissionId",
                table: "PermissionRequests");

            migrationBuilder.AddColumn<bool>(
                name: "CanDelete",
                table: "UserPermissions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "UserPermissions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "UserPermissions",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");
        }
    }
}
