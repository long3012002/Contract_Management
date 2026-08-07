using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XuatXus_Code",
                table: "XuatXus");

            migrationBuilder.DropIndex(
                name: "IX_Resolutions_Code",
                table: "Resolutions");

            migrationBuilder.DropIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns");

            migrationBuilder.DropIndex(
                name: "IX_Licenses_Code",
                table: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_Code",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HangSanXuats_Code",
                table: "HangSanXuats");

            migrationBuilder.DropIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_Code",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DonViTinhs_Code",
                table: "DonViTinhs");

            migrationBuilder.DropIndex(
                name: "IX_DoiTacs_Code",
                table: "DoiTacs");

            migrationBuilder.DropIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns");

            migrationBuilder.DropIndex(
                name: "IX_CongViecLichSuChuyenTieps_Code",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropIndex(
                name: "IX_CongViecGoiThaus_Code",
                table: "CongViecGoiThaus");

            migrationBuilder.RenameIndex(
                name: "IX_CongViecGoiThaus_GoiThauId",
                table: "CongViecGoiThaus",
                newName: "IX_CongViecGoiThau_GoiThauId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "XuatXus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "XuatXus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "XuatXus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Resolutions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Resolutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Resolutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PhanLoaiDuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "PhanLoaiDuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhanLoaiDuAns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "NhomDuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "NhomDuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NhomDuAns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Licenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "Licenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Licenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HopDongs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "HopDongs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HopDongs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HangSanXuats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "HangSanXuats",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HangSanXuats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "HangHoas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "HangHoas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "HangHoas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "GoiThaus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "GoiThaus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "GoiThaus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "FileAttachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "FileAttachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FileAttachments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DuAns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DonViTinhs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DonViTinhs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DonViTinhs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DoiTacs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DoiTacs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DoiTacs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DieuChinhDuAns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DieuChinhDuAns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DieuChinhDuAns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DichVus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "DichVus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DichVus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CongViecNguoiLienQuans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CongViecNguoiLienQuans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CongViecNguoiLienQuans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CongViecLichSuChuyenTieps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CongViecLichSuChuyenTieps",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CongViecLichSuChuyenTieps",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CongViecGoiThaus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CongViecGoiThaus",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CongViecGoiThaus",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CommentCongViecGoiThaus",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByUserId",
                table: "CommentCongViecGoiThaus",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_XuatXus_Code",
                table: "XuatXus",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_Code",
                table: "Resolutions",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Code",
                table: "Licenses",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_Code",
                table: "HopDongs",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_HangSanXuats_Code",
                table: "HangSanXuats",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_Code",
                table: "DuAns",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DonViTinhs_Code",
                table: "DonViTinhs",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DoiTacs_Code",
                table: "DoiTacs",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_Code",
                table: "CongViecLichSuChuyenTieps",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_Code",
                table: "CongViecGoiThaus",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Username_Timestamp",
                table: "AuditLogs",
                columns: new[] { "Username", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XuatXus_Code",
                table: "XuatXus");

            migrationBuilder.DropIndex(
                name: "IX_Resolutions_Code",
                table: "Resolutions");

            migrationBuilder.DropIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns");

            migrationBuilder.DropIndex(
                name: "IX_Licenses_Code",
                table: "Licenses");

            migrationBuilder.DropIndex(
                name: "IX_HopDongs_Code",
                table: "HopDongs");

            migrationBuilder.DropIndex(
                name: "IX_HangSanXuats_Code",
                table: "HangSanXuats");

            migrationBuilder.DropIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_DuAns_Code",
                table: "DuAns");

            migrationBuilder.DropIndex(
                name: "IX_DonViTinhs_Code",
                table: "DonViTinhs");

            migrationBuilder.DropIndex(
                name: "IX_DoiTacs_Code",
                table: "DoiTacs");

            migrationBuilder.DropIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns");

            migrationBuilder.DropIndex(
                name: "IX_CongViecLichSuChuyenTieps_Code",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropIndex(
                name: "IX_CongViecGoiThaus_Code",
                table: "CongViecGoiThaus");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_Username_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "XuatXus");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "XuatXus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "XuatXus");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Resolutions");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Resolutions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Resolutions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhanLoaiDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "NhomDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "NhomDuAns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NhomDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HangSanXuats");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HangSanXuats");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HangSanXuats");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "HangHoas");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HangHoas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "HangHoas");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "GoiThaus");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "GoiThaus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "GoiThaus");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FileAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DuAns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DonViTinhs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DonViTinhs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DonViTinhs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DoiTacs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DoiTacs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DoiTacs");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DieuChinhDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DieuChinhDuAns");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DieuChinhDuAns");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DichVus");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DichVus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DichVus");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CongViecNguoiLienQuans");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CongViecNguoiLienQuans");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CongViecNguoiLienQuans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CongViecLichSuChuyenTieps");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CommentCongViecGoiThaus");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CommentCongViecGoiThaus");

            migrationBuilder.RenameIndex(
                name: "IX_CongViecGoiThau_GoiThauId",
                table: "CongViecGoiThaus",
                newName: "IX_CongViecGoiThaus_GoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_XuatXus_Code",
                table: "XuatXus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_Code",
                table: "Resolutions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Code",
                table: "Licenses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_Code",
                table: "HopDongs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HangSanXuats_Code",
                table: "HangSanXuats",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_Code",
                table: "DuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonViTinhs_Code",
                table: "DonViTinhs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoiTacs_Code",
                table: "DoiTacs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecLichSuChuyenTieps_Code",
                table: "CongViecLichSuChuyenTieps",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_Code",
                table: "CongViecGoiThaus",
                column: "Code",
                unique: true);
        }
    }
}
