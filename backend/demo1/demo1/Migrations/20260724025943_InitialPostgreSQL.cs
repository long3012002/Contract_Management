using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSQL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TableName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    ChangedColumns = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => new { x.Id, x.Timestamp });
                });

            migrationBuilder.CreateTable(
                name: "ChucVus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenChucVu = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoiTacs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Account = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Representative = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Position = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoiTacs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DonVis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenDonVi = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonVis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhomDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomDuAns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhanLoaiDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanLoaiDuAns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhongBans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenPhongBan = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    IdPhongBan = table.Column<Guid>(type: "uuid", nullable: true),
                    IdChucVu = table.Column<Guid>(type: "uuid", nullable: true),
                    IdDonVi = table.Column<Guid>(type: "uuid", nullable: true),
                    TenPhongBan = table.Column<string>(type: "text", nullable: true),
                    TenChucVu = table.Column<string>(type: "text", nullable: true),
                    TenDonVi = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenHash = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuToanPheDuyet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
                    LoaiDuAn = table.Column<int>(type: "integer", nullable: false),
                    NhomDuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    PhanLoaiDuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    NguonDuAnIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ChuDauTu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DiaDiemThucHien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThoiGianThucHien = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    NoiDung = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HinhThucQuanLy = table.Column<int>(type: "integer", nullable: true),
                    ToChucThucHien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NamBatDau = table.Column<int>(type: "integer", nullable: true),
                    NamKetThuc = table.Column<int>(type: "integer", nullable: true),
                    DaKetThuc = table.Column<bool>(type: "boolean", nullable: false),
                    DaTrienKhai = table.Column<bool>(type: "boolean", nullable: true),
                    SoQuyetDinh = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuAns_NhomDuAns_NhomDuAnId",
                        column: x => x.NhomDuAnId,
                        principalTable: "NhomDuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DuAns_PhanLoaiDuAns_PhanLoaiDuAnId",
                        column: x => x.PhanLoaiDuAnId,
                        principalTable: "PhanLoaiDuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DieuChinhDuAns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    GiaTriDieuChinh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LyDoDieuChinh = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    NgayDieuChinh = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "GoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    GiaTriGoiThau = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NguongCanhBaoPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoiThaus_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeatureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongViecGoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoiThauId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stt = table.Column<int>(type: "integer", nullable: false),
                    TenTaiLieu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NgayKy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoaiVanBan = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TinhTrang = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongViecGoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongViecGoiThaus_GoiThaus_GoiThauId",
                        column: x => x.GoiThauId,
                        principalTable: "GoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HopDongs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoiThauId = table.Column<Guid>(type: "uuid", nullable: true),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChuDauTuId = table.Column<Guid>(type: "uuid", nullable: true),
                    NhaThauId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoaiHopDong = table.Column<int>(type: "integer", nullable: false),
                    ThoiHanThucHien = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DiaDiemThucHien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GiaTriHopDong = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HinhThucThanhToan = table.Column<int>(type: "integer", nullable: false),
                    NgayHieuLuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenewalReminderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRenewalRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HopDongs_DoiTacs_ChuDauTuId",
                        column: x => x.ChuDauTuId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HopDongs_DoiTacs_NhaThauId",
                        column: x => x.NhaThauId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HopDongs_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HopDongs_GoiThaus_GoiThauId",
                        column: x => x.GoiThauId,
                        principalTable: "GoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PermissionRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: true),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedPermissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FeatureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EntityTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestedAction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Permissions_RequestedPermissionId",
                        column: x => x.RequestedPermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_UserPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "UserPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PermissionRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentCongViecGoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongViecGoiThauId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentCongViecGoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentCongViecGoiThaus_CommentCongViecGoiThaus_ParentComme~",
                        column: x => x.ParentCommentId,
                        principalTable: "CommentCongViecGoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommentCongViecGoiThaus_CongViecGoiThaus_CongViecGoiThauId",
                        column: x => x.CongViecGoiThauId,
                        principalTable: "CongViecGoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentCongViecGoiThaus_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CongViecNguoiLienQuans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CongViecGoiThauId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrangThaiXacNhan = table.Column<string>(type: "text", nullable: false),
                    HanXacNhanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    XacNhanAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoaiXacNhan = table.Column<string>(type: "text", nullable: true),
                    ReminderJobIds = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongViecNguoiLienQuans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CongViecNguoiLienQuans_CongViecGoiThaus_CongViecGoiThauId",
                        column: x => x.CongViecGoiThauId,
                        principalTable: "CongViecGoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CongViecNguoiLienQuans_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DotThanhToans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HopDongId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenDot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TyLeThanhToan = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    GiaTriThanhToan = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DieuKienThanhToan = table.Column<string>(type: "text", nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotThanhToans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotThanhToans_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DuAnId = table.Column<Guid>(type: "uuid", nullable: false),
                    HopDongId = table.Column<Guid>(type: "uuid", nullable: true),
                    NhaCungCapId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoaiLicense = table.Column<int>(type: "integer", nullable: false),
                    SoLuong = table.Column<int>(type: "integer", nullable: true),
                    ThongTinThietBi = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanhBaoTruocNgay = table.Column<int>(type: "integer", nullable: false),
                    TrangThai = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_Licenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Licenses_DoiTacs_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Licenses_DuAns_DuAnId",
                        column: x => x.DuAnId,
                        principalTable: "DuAns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenses_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NhaThauGoiThaus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HopDongId = table.Column<Guid>(type: "uuid", nullable: false),
                    NhaThauId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsLienDanh = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaThauGoiThaus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhaThauGoiThaus_DoiTacs_NhaThauId",
                        column: x => x.NhaThauId,
                        principalTable: "DoiTacs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhaThauGoiThaus_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommentMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentionedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentMentions_CommentCongViecGoiThaus_CommentId",
                        column: x => x.CommentId,
                        principalTable: "CommentCongViecGoiThaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommentMentions_Users_MentionedUserId",
                        column: x => x.MentionedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "VIEW", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quyền xem dữ liệu", "Xem" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "CREATE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quyền tạo mới dữ liệu", "Tạo mới" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "EDIT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quyền chỉnh sửa bản ghi", "Chỉnh sửa" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "DELETE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quyền xóa bản ghi", "Xóa" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "APPROVE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quyền phê duyệt yêu cầu", "Phê duyệt" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommentCongViecGoiThaus_CongViecGoiThauId",
                table: "CommentCongViecGoiThaus",
                column: "CongViecGoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentCongViecGoiThaus_ParentCommentId",
                table: "CommentCongViecGoiThaus",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentCongViecGoiThaus_UserId",
                table: "CommentCongViecGoiThaus",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentMentions_CommentId",
                table: "CommentMentions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentMentions_MentionedUserId",
                table: "CommentMentions",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_Code",
                table: "CongViecGoiThaus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongViecGoiThaus_GoiThauId",
                table: "CongViecGoiThaus",
                column: "GoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecNguoiLienQuans_Code",
                table: "CongViecNguoiLienQuans",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecNguoiLienQuans_CongViecGoiThauId",
                table: "CongViecNguoiLienQuans",
                column: "CongViecGoiThauId");

            migrationBuilder.CreateIndex(
                name: "IX_CongViecNguoiLienQuans_UserId",
                table: "CongViecNguoiLienQuans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_Code",
                table: "DieuChinhDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhDuAns_DuAnId",
                table: "DieuChinhDuAns",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DoiTacs_Code",
                table: "DoiTacs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DotThanhToans_HopDongId",
                table: "DotThanhToans",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_Code",
                table: "DuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_NhomDuAnId",
                table: "DuAns",
                column: "NhomDuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_DuAns_PhanLoaiDuAnId",
                table: "DuAns",
                column: "PhanLoaiDuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_Code",
                table: "GoiThaus",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiThaus_DuAnId",
                table: "GoiThaus",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_ChuDauTuId",
                table: "HopDongs",
                column: "ChuDauTuId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_Code",
                table: "HopDongs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_DuAnId",
                table: "HopDongs",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_GoiThauId",
                table: "HopDongs",
                column: "GoiThauId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_NhaThauId",
                table: "HopDongs",
                column: "NhaThauId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_Code",
                table: "Licenses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_DuAnId",
                table: "Licenses",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_HopDongId",
                table: "Licenses",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_NhaCungCapId",
                table: "Licenses",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_HopDongId",
                table: "NhaThauGoiThaus",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_NhaThauGoiThaus_NhaThauId",
                table: "NhaThauGoiThaus",
                column: "NhaThauId");

            migrationBuilder.CreateIndex(
                name: "IX_NhomDuAns_Code",
                table: "NhomDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_DuAnId",
                table: "PermissionRequests",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_PermissionId",
                table: "PermissionRequests",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_RequestedPermissionId",
                table: "PermissionRequests",
                column: "RequestedPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_ReviewerId",
                table: "PermissionRequests",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionRequests_UserId",
                table: "PermissionRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanLoaiDuAns_Code",
                table: "PhanLoaiDuAns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resolutions_Code",
                table: "Resolutions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_User_DuAn_Perm",
                table: "UserPermissions",
                columns: new[] { "UserId", "DuAnId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermission_User_Entity_Perm",
                table: "UserPermissions",
                columns: new[] { "UserId", "EntityName", "EntityId", "PermissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_DuAnId",
                table: "UserPermissions",
                column: "DuAnId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GrantedByUserId",
                table: "UserPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ChucVus");

            migrationBuilder.DropTable(
                name: "CommentMentions");

            migrationBuilder.DropTable(
                name: "CongViecNguoiLienQuans");

            migrationBuilder.DropTable(
                name: "DieuChinhDuAns");

            migrationBuilder.DropTable(
                name: "DonVis");

            migrationBuilder.DropTable(
                name: "DotThanhToans");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "NhaThauGoiThaus");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PermissionRequests");

            migrationBuilder.DropTable(
                name: "PhongBans");

            migrationBuilder.DropTable(
                name: "Resolutions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "CommentCongViecGoiThaus");

            migrationBuilder.DropTable(
                name: "HopDongs");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CongViecGoiThaus");

            migrationBuilder.DropTable(
                name: "DoiTacs");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "GoiThaus");

            migrationBuilder.DropTable(
                name: "DuAns");

            migrationBuilder.DropTable(
                name: "NhomDuAns");

            migrationBuilder.DropTable(
                name: "PhanLoaiDuAns");
        }
    }
}
