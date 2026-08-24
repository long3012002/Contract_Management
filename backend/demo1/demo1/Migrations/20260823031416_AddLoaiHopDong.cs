using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddLoaiHopDong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LoaiHopDongId",
                table: "HopDongs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoaiHopDongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiHopDongs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_LoaiHopDongId",
                table: "HopDongs",
                column: "LoaiHopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_LoaiHopDongs_Code",
                table: "LoaiHopDongs",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_HopDongs_LoaiHopDongs_LoaiHopDongId",
                table: "HopDongs",
                column: "LoaiHopDongId",
                principalTable: "LoaiHopDongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HopDongs_LoaiHopDongs_LoaiHopDongId",
                table: "HopDongs");

            migrationBuilder.DropTable(
                name: "LoaiHopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_LoaiHopDongId",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "LoaiHopDongId",
                table: "HopDongs");
        }
    }
}
