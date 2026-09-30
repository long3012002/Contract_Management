using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRetentionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
