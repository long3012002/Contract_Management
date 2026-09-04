using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDuAnNguonTrienKhaiRel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NguonDuAnIds",
                table: "DuAns");

            migrationBuilder.CreateTable(
                name: "DuAnNguonTrienKhais",
                columns: table => new
                {
                    TrienKhaiProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    NguonProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAnNguonTrienKhais", x => new { x.TrienKhaiProjectId, x.NguonProjectId });
                    table.ForeignKey(
                        name: "FK_DuAnNguonTrienKhais_DuAns_NguonProjectId",
                        column: x => x.NguonProjectId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuAnNguonTrienKhais_DuAns_TrienKhaiProjectId",
                        column: x => x.TrienKhaiProjectId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonTrienKhais_NguonProjectId",
                table: "DuAnNguonTrienKhais",
                column: "NguonProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuAnNguonTrienKhais");

            migrationBuilder.AddColumn<string>(
                name: "NguonDuAnIds",
                table: "DuAns",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }
    }
}
