using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddCongViecLichSuChuyenTiep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CongViecLichSuChuyenTieps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongViecGoiThauId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongViecLichSuChuyenTieps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongViecLichSuChuyenTieps_CongViecGoiThaus_CongViecGoiThauId",
                        column: x => x.CongViecGoiThauId,
                        principalTable: "CongViecGoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongViecLichSuChuyenTieps_Users_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CongViecLichSuChuyenTieps_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_Code",
                table: "CongViecLichSuChuyenTieps",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_CongViecGoiThauId",
                table: "CongViecLichSuChuyenTieps",
                column: "CongViecGoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_FromUserId",
                table: "CongViecLichSuChuyenTieps",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_ToUserId",
                table: "CongViecLichSuChuyenTieps",
                column: "ToUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CongViecLichSuChuyenTieps");
        }
    }
}
