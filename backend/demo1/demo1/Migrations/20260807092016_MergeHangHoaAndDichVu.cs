using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class MergeHangHoaAndDichVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DichVus");

            migrationBuilder.DropTable(
                name: "HangHoas");

            migrationBuilder.CreateTable(
                name: "HangHoaDichVus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdParent = table.Column<Guid>(type: "uuid", nullable: false),
                    Stt = table.Column<string>(type: "text", nullable: true),
                    Loai = table.Column<int>(type: "integer", nullable: false),
                    DanhMucHangHoa = table.Column<string>(type: "text", nullable: true),
                    KyMaHieu = table.Column<string>(type: "text", nullable: true),
                    NhanHieu = table.Column<string>(type: "text", nullable: true),
                    NamSanXuat = table.Column<string>(type: "text", nullable: true),
                    IdXuatXu = table.Column<Guid>(type: "uuid", nullable: true),
                    IdHangSanXuat = table.Column<Guid>(type: "uuid", nullable: true),
                    CauHinhTinhNangKyThuatCoBan = table.Column<string>(type: "text", nullable: true),
                    IdLicense = table.Column<Guid>(type: "uuid", nullable: true),
                    IdDonViTinh = table.Column<Guid>(type: "uuid", nullable: true),
                    KhoiLuong = table.Column<int>(type: "integer", nullable: false),
                    MaHS = table.Column<string>(type: "text", nullable: true),
                    TenDichVu = table.Column<string>(type: "text", nullable: true),
                    MoTaDichVu = table.Column<string>(type: "text", nullable: true),
                    DiaDiemThucHienDichVu = table.Column<string>(type: "text", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThoiHan = table.Column<string>(type: "text", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayHoanThanhDichVu = table.Column<string>(type: "text", nullable: true),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_HangHoaDichVus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HangHoaDichVus_DonViTinhs_IdDonViTinh",
                        column: x => x.IdDonViTinh,
                        principalTable: "DonViTinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoaDichVus_HangSanXuats_IdHangSanXuat",
                        column: x => x.IdHangSanXuat,
                        principalTable: "HangSanXuats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoaDichVus_HopDongs_IdParent",
                        column: x => x.IdParent,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HangHoaDichVus_Licenses_IdLicense",
                        column: x => x.IdLicense,
                        principalTable: "Licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoaDichVus_XuatXus_IdXuatXu",
                        column: x => x.IdXuatXu,
                        principalTable: "XuatXus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_Code",
                table: "HangHoaDichVus",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_IdDonViTinh",
                table: "HangHoaDichVus",
                column: "IdDonViTinh");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_IdHangSanXuat",
                table: "HangHoaDichVus",
                column: "IdHangSanXuat");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_IdLicense",
                table: "HangHoaDichVus",
                column: "IdLicense");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_IdParent",
                table: "HangHoaDichVus",
                column: "IdParent");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoaDichVus_IdXuatXu",
                table: "HangHoaDichVus",
                column: "IdXuatXu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HangHoaDichVus");

            migrationBuilder.CreateTable(
                name: "DichVus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdDonViTinh = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DiaDiemThucHienDichVu = table.Column<string>(type: "text", nullable: true),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IdParent = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    KhoiLuong = table.Column<int>(type: "integer", nullable: false),
                    MoTaDichVu = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayHoanThanhDichVu = table.Column<string>(type: "text", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Stt = table.Column<string>(type: "text", nullable: true),
                    TenDichVu = table.Column<string>(type: "text", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ThoiHan = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DichVus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DichVus_DonViTinhs_IdDonViTinh",
                        column: x => x.IdDonViTinh,
                        principalTable: "DonViTinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DichVus_HopDongs_IdParent",
                        column: x => x.IdParent,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HangHoas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdDonViTinh = table.Column<Guid>(type: "uuid", nullable: true),
                    IdHangSanXuat = table.Column<Guid>(type: "uuid", nullable: true),
                    IdLicense = table.Column<Guid>(type: "uuid", nullable: true),
                    IdXuatXu = table.Column<Guid>(type: "uuid", nullable: true),
                    CauHinhTinhNangKyThuatCoBan = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DanhMucHangHoa = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IdParent = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    KhoiLuong = table.Column<int>(type: "integer", nullable: false),
                    KyMaHieu = table.Column<string>(type: "text", nullable: true),
                    MaHS = table.Column<string>(type: "text", nullable: true),
                    NamSanXuat = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NhanHieu = table.Column<string>(type: "text", nullable: true),
                    Stt = table.Column<string>(type: "text", nullable: true),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangHoas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HangHoas_DonViTinhs_IdDonViTinh",
                        column: x => x.IdDonViTinh,
                        principalTable: "DonViTinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoas_HangSanXuats_IdHangSanXuat",
                        column: x => x.IdHangSanXuat,
                        principalTable: "HangSanXuats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoas_HopDongs_IdParent",
                        column: x => x.IdParent,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HangHoas_Licenses_IdLicense",
                        column: x => x.IdLicense,
                        principalTable: "Licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HangHoas_XuatXus_IdXuatXu",
                        column: x => x.IdXuatXu,
                        principalTable: "XuatXus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DichVus_IdDonViTinh",
                table: "DichVus",
                column: "IdDonViTinh");

            migrationBuilder.CreateIndex(
                name: "IX_DichVus_IdParent",
                table: "DichVus",
                column: "IdParent");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoas_IdDonViTinh",
                table: "HangHoas",
                column: "IdDonViTinh");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoas_IdHangSanXuat",
                table: "HangHoas",
                column: "IdHangSanXuat");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoas_IdLicense",
                table: "HangHoas",
                column: "IdLicense");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoas_IdParent",
                table: "HangHoas",
                column: "IdParent");

            migrationBuilder.CreateIndex(
                name: "IX_HangHoas_IdXuatXu",
                table: "HangHoas",
                column: "IdXuatXu");
        }
    }
}
