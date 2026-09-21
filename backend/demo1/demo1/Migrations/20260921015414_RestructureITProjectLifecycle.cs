using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RestructureITProjectLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DieuChinhDuAns");

            migrationBuilder.DropTable(
                name: "DuAnNguonTrienKhais");

            migrationBuilder.DropColumn(
                name: "LoaiDuAn",
                table: "DuAns");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "DotThanhToans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DuAnGopLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetDuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    NgayGop = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguoiThucHienId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuToanLucGop = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_DuAnGopLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuAnGopLinks_DuAns_SourceDuAnId",
                        column: x => x.SourceDuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuAnGopLinks_DuAns_TargetDuAnId",
                        column: x => x.TargetDuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DuAnGopLinks_Users_NguoiThucHienId",
                        column: x => x.NguoiThucHienId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeHoachVons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NamKeHoach = table.Column<int>(type: "integer", nullable: false),
                    LoaiKeHoach = table.Column<int>(type: "integer", nullable: false),
                    DotBoSung = table.Column<int>(type: "integer", nullable: true),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    SoQuyetDinh = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NgayPheDuyet = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TongMucDeNghi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TongMucDuocDuyet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_KeHoachVons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeHoachVons_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KeHoachVonDuAns",
                columns: table => new
                {
                    KeHoachVonId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    SoTienDeNghi = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SoTienDuocDuyet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    VonDieuLe = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    QuyDauTuPhatTrien = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeHoachVonDuAns", x => new { x.KeHoachVonId, x.DuAnId });
                    table.ForeignKey(
                        name: "FK_KeHoachVonDuAns_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeHoachVonDuAns_KeHoachVons_KeHoachVonId",
                        column: x => x.KeHoachVonId,
                        principalTable: "KeHoachVons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLink_CreatedAt_Id_Desc",
                table: "DuAnGopLinks",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLinks_Code",
                table: "DuAnGopLinks",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLinks_NguoiThucHienId",
                table: "DuAnGopLinks",
                column: "NguoiThucHienId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLinks_SourceDuAnId",
                table: "DuAnGopLinks",
                column: "SourceDuAnId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAnGopLinks_TargetDuAnId",
                table: "DuAnGopLinks",
                column: "TargetDuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVonDuAns_DuAnId",
                table: "KeHoachVonDuAns",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVon_CreatedAt_Id_Desc",
                table: "KeHoachVons",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVons_Code",
                table: "KeHoachVons",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVons_CreatedByUserId",
                table: "KeHoachVons",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuAnGopLinks");

            migrationBuilder.DropTable(
                name: "KeHoachVonDuAns");

            migrationBuilder.DropTable(
                name: "KeHoachVons");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "DotThanhToans");

            migrationBuilder.AddColumn<int>(
                name: "LoaiDuAn",
                table: "DuAns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DieuChinhDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    GiaTriDieuChinh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LyDoDieuChinh = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NgayDieuChinh = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuChinhDuAns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DieuChinhDuAns_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DuAnNguonTrienKhais",
                columns: table => new
                {
                    TrienKhaiProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NguonProjectId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAnNguonTrienKhais", x => x.TrienKhaiProjectId);
                    table.ForeignKey(
                        name: "FK_DuAnNguonTrienKhais_DuAns_TrienKhaiProjectId",
                        column: x => x.TrienKhaiProjectId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAn_CreatedAt_Id_Desc",
                table: "DieuChinhDuAns",
                columns: new[] { "CreatedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_DuAnId",
                table: "DieuChinhDuAns",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAnNguonTrienKhais_TrienKhaiProjectId",
                table: "DuAnNguonTrienKhais",
                column: "TrienKhaiProjectId",
                unique: true);
        }
    }
}
