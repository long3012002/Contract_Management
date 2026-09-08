using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddDuAnNguonVonTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuAnNguonVons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    NguonVonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoTien = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAnNguonVons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuAnNguonVons_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuAnNguonVons_NguonVons_NguonVonId",
                        column: x => x.NguonVonId,
                        principalTable: "NguonVons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonVons_DuAnId",
                table: "DuAnNguonVons",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonVons_NguonVonId",
                table: "DuAnNguonVons",
                column: "NguonVonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuAnNguonVons");
        }
    }
}
