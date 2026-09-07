using Microsoft.EntityFrameworkCore;
using demo1.Entity;
using demo1.Entity.DanhMuc;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using demo1.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace demo1.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;
        public ICurrentUserService? CurrentUserService => _currentUserService;

        private static readonly System.Collections.Generic.HashSet<string> IgnoredAuditProperties = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "CreatedAt",
            "CreatedBy",
            "CreatedByUserId",
            "UpdatedAt",
            "UpdatedBy",
            "UpdatedByUserId"
        };

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService? currentUserService = null) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<DuAn> DuAns { get; set; } = null!;
        public DbSet<DuAnNguonTrienKhai> DuAnNguonTrienKhais { get; set; } = null!;
        public DbSet<NhomDuAn> NhomDuAns { get; set; } = null!;
        public DbSet<PhanLoaiDuAn> PhanLoaiDuAns { get; set; } = null!;
        public DbSet<NguonVon> NguonVons { get; set; } = null!;
        public DbSet<LoaiHopDong> LoaiHopDongs { get; set; } = null!;
        public DbSet<GoiThau> GoiThaus { get; set; } = null!;
        public DbSet<DieuChinhDuAn> DieuChinhDuAns { get; set; } = null!;
        public DbSet<HopDong> HopDongs { get; set; } = null!;
        public DbSet<PhuLucHopDong> PhuLucHopDongs { get; set; } = null!;
        public DbSet<DoiTac> DoiTacs { get; set; } = null!;
        public DbSet<DotThanhToan> DotThanhToans { get; set; } = null!;
        public DbSet<Resolution> Resolutions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        public DbSet<PhongBan> PhongBans { get; set; } = null!;
        public DbSet<ToNhom> ToNhoms { get; set; } = null!;
        public DbSet<ChucVu> ChucVus { get; set; } = null!;
        public DbSet<DonVi> DonVis { get; set; } = null!;
        public DbSet<UserPermission> UserPermissions { get; set; } = null!;
        public DbSet<PermissionRequest> PermissionRequests { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NhaThauGoiThau> NhaThauGoiThaus { get; set; } = null!;
        public DbSet<CongViecGoiThau> CongViecGoiThaus { get; set; } = null!;
        public DbSet<License> Licenses { get; set; } = null!;
        public DbSet<CommentCongViecGoiThau> CommentCongViecGoiThaus { get; set; } = null!;
        public DbSet<CommentMention> CommentMentions { get; set; } = null!;
        public DbSet<CongViecNguoiLienQuan> CongViecNguoiLienQuans { get; set; } = null!;
        public DbSet<CongViecLichSuChuyenTiep> CongViecLichSuChuyenTieps { get; set; } = null!;
        public DbSet<HangHoaDichVu> HangHoaDichVus { get; set; } = null!;
        public DbSet<XuatXu> XuatXus { get; set; } = null!;
        public DbSet<DonViTinh> DonViTinhs { get; set; } = null!;
        public DbSet<HangSanXuat> HangSanXuats { get; set; } = null!;
        public DbSet<FileAttachment> FileAttachments { get; set; } = null!;
        public DbSet<FileVersion> FileVersions { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime?, DateTime?>(
                v => !v.HasValue ? v : (v.Value.Kind == DateTimeKind.Utc ? v : v.Value.ToUniversalTime()),
                v => !v.HasValue ? v : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            ConfigureBaseEntity(modelBuilder.Entity<DuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<NhomDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<PhanLoaiDuAn>());
            modelBuilder.Entity<LoaiHopDong>(entity =>
            {
                ConfigureBaseEntity(entity);
                entity.HasData(
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        Code = "01",
                        Name = "Bảo trì",
                        Description = "Hợp đồng bảo trì (Mặc định)",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                        Code = "02",
                        Name = "Mua sắm phần cứng",
                        Description = "Hợp đồng mua sắm thiết bị, phần cứng",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                        Code = "03",
                        Name = "Bản quyền phần mềm",
                        Description = "Hợp đồng mua sắm bản quyền, phần mềm",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        Code = "04",
                        Name = "Tư vấn",
                        Description = "Hợp đồng tư vấn (lập dự án, thẩm định, giám sát)",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                        Code = "05",
                        Name = "Thuê dịch vụ",
                        Description = "Hợp đồng thuê dịch vụ (đường truyền, cloud, server)",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new LoaiHopDong
                    {
                        Id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                        Code = "99",
                        Name = "Khác",
                        Description = "Các loại hợp đồng khác",
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                );
            });
            ConfigureBaseEntity(modelBuilder.Entity<DieuChinhDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<GoiThau>());
            ConfigureBaseEntity(modelBuilder.Entity<HopDong>());
            ConfigureBaseEntity(modelBuilder.Entity<PhuLucHopDong>());
            ConfigureBaseEntity(modelBuilder.Entity<DoiTac>());
            ConfigureBaseEntity(modelBuilder.Entity<Resolution>());
            modelBuilder.Entity<NhaThauGoiThau>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            ConfigureBaseEntity(modelBuilder.Entity<CongViecGoiThau>());
            ConfigureBaseEntity(modelBuilder.Entity<License>());
            ConfigureBaseEntity(modelBuilder.Entity<CongViecLichSuChuyenTiep>());
            modelBuilder.Entity<CongViecNguoiLienQuan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ReminderJobIds).HasMaxLength(1000);

                entity.HasOne(nlq => nlq.CongViecGoiThau)
                    .WithMany(cv => cv.NguoiLienQuans)
                    .HasForeignKey(nlq => nlq.CongViecGoiThauId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(nlq => nlq.User)
                    .WithMany()
                    .HasForeignKey(nlq => nlq.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Code).IsUnique(false);
            });

            modelBuilder.Entity<CongViecLichSuChuyenTiep>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.GhiChu).HasMaxLength(2000);

                entity.HasOne(ls => ls.CongViecGoiThau)
                    .WithMany(cv => cv.LichSuChuyenTieps)
                    .HasForeignKey(ls => ls.CongViecGoiThauId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ls => ls.FromUser)
                    .WithMany()
                    .HasForeignKey(ls => ls.FromUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ls => ls.ToUser)
                    .WithMany()
                    .HasForeignKey(ls => ls.ToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure NhaThauGoiThau relationship


            modelBuilder.Entity<NhaThauGoiThau>()
                .HasOne(ntgt => ntgt.HopDong)
                .WithMany(hd => hd.NhaThauGoiThaus)
                .HasForeignKey(ntgt => ntgt.HopDongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NhaThauGoiThau>()
                .HasOne(ntgt => ntgt.NhaThau)
                .WithMany()
                .HasForeignKey(ntgt => ntgt.NhaThauId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<DuAn>()
                .Property(da => da.DuToanPheDuyet)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DuAn>()
                .HasOne(da => da.NhomDuAn)
                .WithMany()
                .HasForeignKey(da => da.NhomDuAnId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAn>()
                .HasOne(da => da.PhanLoaiDuAn)
                .WithMany()
                .HasForeignKey(da => da.PhanLoaiDuAnId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAn>()
                .HasOne(da => da.NguonVon)
                .WithMany()
                .HasForeignKey(da => da.NguonVonId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAn>()
                .HasOne(da => da.CreatedByUser)
                .WithMany()
                .HasForeignKey(da => da.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DuAn>()
                .Property(da => da.ChuDauTu)
                .HasMaxLength(255);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.DiaDiemThucHien)
                .HasMaxLength(500);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.ThoiGianThucHien)
                .HasMaxLength(255);
            modelBuilder.Entity<DuAnNguonTrienKhai>(entity =>
            {
                entity.HasKey(e => e.TrienKhaiProjectId);
                entity.HasIndex(e => e.TrienKhaiProjectId).IsUnique();

                entity.Property(e => e.NguonProjectId)
                      .HasMaxLength(2000);

                entity.HasOne(e => e.TrienKhaiProject)
                      .WithMany(p => p.NguonDuAns)
                      .HasForeignKey(e => e.TrienKhaiProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DuAn>()
                .Property(da => da.NoiDung)
                .HasMaxLength(2000);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.ToChucThucHien)
                .HasMaxLength(500);

            modelBuilder.Entity<DieuChinhDuAn>()
                .Property(dc => dc.GiaTriDieuChinh)
                .HasPrecision(18, 2);
            modelBuilder.Entity<DieuChinhDuAn>()
                .Property(dc => dc.LyDoDieuChinh)
                .HasMaxLength(1000);
            modelBuilder.Entity<DieuChinhDuAn>()
                .HasOne(dc => dc.DuAn)
                .WithMany(da => da.DieuChinhs)
                .HasForeignKey(dc => dc.DuAnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GoiThau>()
                .Property(gt => gt.GiaTriGoiThau)
                .HasPrecision(18, 2);
            modelBuilder.Entity<GoiThau>()
                .HasOne(gt => gt.DuAn)
                .WithMany(da => da.GoiThaus)
                .HasForeignKey(gt => gt.DuAnId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CongViecGoiThau>()
                .Property(cv => cv.TenTaiLieu)
                .HasMaxLength(500)
                .IsRequired();
            modelBuilder.Entity<CongViecGoiThau>()
                .Property(cv => cv.LoaiVanBan)
                .HasMaxLength(100);
            modelBuilder.Entity<CongViecGoiThau>()
                .Property(cv => cv.TinhTrang)
                .HasMaxLength(100);
            modelBuilder.Entity<CongViecGoiThau>()
                .Property(cv => cv.GhiChu)
                .HasMaxLength(1000);
            modelBuilder.Entity<CongViecGoiThau>()
                .HasOne(cv => cv.GoiThau)
                .WithMany(gt => gt.CongViecGoiThaus)
                .HasForeignKey(cv => cv.GoiThauId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CongViecGoiThau>()
                .HasIndex(cv => cv.GoiThauId)
                .HasDatabaseName("IX_CongViecGoiThau_GoiThauId");

            modelBuilder.Entity<CongViecGoiThau>()
                .HasOne(cv => cv.CreateUser)
                .WithMany()
                .HasForeignKey(cv => cv.CreateUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CongViecGoiThau>()
                .HasOne(cv => cv.ModifiedUser)
                .WithMany()
                .HasForeignKey(cv => cv.ModifiedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<License>()
                .Property(l => l.ThongTinThietBi)
                .HasMaxLength(1000);
            modelBuilder.Entity<License>()
                .Property(l => l.GhiChu)
                .HasMaxLength(2000);
            modelBuilder.Entity<License>()
                .HasOne(l => l.DuAn)
                .WithMany(da => da.Licenses)
                .HasForeignKey(l => l.DuAnId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<License>()
                .HasOne(l => l.HopDong)
                .WithMany()
                .HasForeignKey(l => l.HopDongId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<License>()
                .HasOne(l => l.NhaCungCap)
                .WithMany()
                .HasForeignKey(l => l.NhaCungCapId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HopDong>()
                .Property(hd => hd.GiaTriHopDong)
                .HasPrecision(18, 2);
            modelBuilder.Entity<HopDong>()
                .Property(hd => hd.ThoiHanThucHien)
                .HasMaxLength(255);
            modelBuilder.Entity<HopDong>()
                .Property(hd => hd.DiaDiemThucHien)
                .HasMaxLength(500);

            // Configure unique index on GoiThauId for 1-to-1 relationship
            modelBuilder.Entity<HopDong>()
                .HasIndex(hd => hd.GoiThauId)
                .IsUnique();

            modelBuilder.Entity<HopDong>()
                .HasOne(hd => hd.GoiThau)
                .WithMany()
                .HasForeignKey(hd => hd.GoiThauId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HopDong>()
                .HasOne(hd => hd.ChuDauTu)
                .WithMany()
                .HasForeignKey(hd => hd.ChuDauTuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HopDong>()
                .HasOne(hd => hd.NhaThau)
                .WithMany()
                .HasForeignKey(hd => hd.NhaThauId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HopDong>()
                .HasOne(hd => hd.DuAn)
                .WithMany()
                .HasForeignKey(hd => hd.DuAnId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HopDong>()
                .HasOne(hd => hd.LoaiHopDongNavigation)
                .WithMany()
                .HasForeignKey(hd => hd.LoaiHopDongId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PhuLucHopDong>(entity =>
            {
                entity.Property(p => p.GiaTriDieuChinh)
                    .HasPrecision(18, 2);
                entity.Property(p => p.SoPhuLuc)
                    .HasMaxLength(100)
                    .IsRequired();
                entity.Property(p => p.TenPhuLuc)
                    .HasMaxLength(255);
                entity.HasOne(p => p.HopDong)
                    .WithMany(h => h.PhuLucHopDongs)
                    .HasForeignKey(p => p.HopDongId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.TaxCode)
                .HasMaxLength(50);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Phone)
                .HasMaxLength(30);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Email)
                .HasMaxLength(255);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Address)
                .HasMaxLength(500);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Account)
                .HasMaxLength(100);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Representative)
                .HasMaxLength(255);
            modelBuilder.Entity<DoiTac>()
                .Property(dt => dt.Position)
                .HasMaxLength(255);

            modelBuilder.Entity<DotThanhToan>()
                .HasKey(d => d.Id);
            modelBuilder.Entity<DotThanhToan>()
                .Property(d => d.TenDot)
                .HasMaxLength(255)
                .IsRequired();
            modelBuilder.Entity<DotThanhToan>()
                .Property(d => d.TyLeThanhToan)
                .HasPrecision(5, 2);
            modelBuilder.Entity<DotThanhToan>()
                .Property(d => d.GiaTriThanhToan)
                .HasPrecision(18, 2);
            modelBuilder.Entity<DotThanhToan>()
                .HasOne(d => d.HopDong)
                .WithMany(h => h.DotThanhToans)
                .HasForeignKey(d => d.HopDongId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DotThanhToan>()
                .HasOne(d => d.PhuLucHopDong)
                .WithMany(p => p.DotThanhToans)
                .HasForeignKey(d => d.PhuLucHopDongId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Resolution>()
                .Property(resolution => resolution.FileUrl)
                .HasMaxLength(1000);

            // Configure composite key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure ToNhom relationship
            modelBuilder.Entity<ToNhom>()
                .HasOne(t => t.PhongBan)
                .WithMany()
                .HasForeignKey(t => t.IdPhongBan)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Permission catalog entity
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Code).HasMaxLength(50).IsRequired();
                entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.HasIndex(p => p.Code).IsUnique();

                var fixedCreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                entity.HasData(
                    new Permission { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "VIEW", Name = "Xem", Description = "Quyền xem dữ liệu", CreatedAt = fixedCreatedAt },
                    new Permission { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "CREATE", Name = "Tạo mới", Description = "Quyền tạo mới dữ liệu", CreatedAt = fixedCreatedAt },
                    new Permission { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "EDIT", Name = "Chỉnh sửa", Description = "Quyền chỉnh sửa bản ghi", CreatedAt = fixedCreatedAt },
                    new Permission { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Code = "DELETE", Name = "Xóa", Description = "Quyền xóa bản ghi", CreatedAt = fixedCreatedAt },
                    new Permission { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Code = "APPROVE", Name = "Phê duyệt", Description = "Quyền phê duyệt yêu cầu", CreatedAt = fixedCreatedAt }
                );
            });

            // Configure UserPermission entity
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(up => up.Id);
                entity.Property(up => up.FeatureCode).HasMaxLength(100).IsRequired();
                entity.Property(up => up.EntityName).HasMaxLength(100).IsRequired();
                entity.Property(up => up.EntityId).HasMaxLength(255).IsRequired();
                entity.HasOne(up => up.User)
                    .WithMany()
                    .HasForeignKey(up => up.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(up => up.GrantedByUser)
                    .WithMany()
                    .HasForeignKey(up => up.GrantedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(up => up.Permission)
                    .WithMany()
                    .HasForeignKey(up => up.PermissionId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(up => up.DuAn)
                    .WithMany()
                    .HasForeignKey(up => up.DuAnId)
                    .OnDelete(DeleteBehavior.Cascade);

                // High-performance Composite Indexes
                entity.HasIndex(up => new { up.UserId, up.DuAnId, up.PermissionId })
                    .HasDatabaseName("IX_UserPermission_User_DuAn_Perm");
                entity.HasIndex(up => new { up.UserId, up.EntityName, up.EntityId, up.PermissionId })
                    .HasDatabaseName("IX_UserPermission_User_Entity_Perm");
            });

            // Configure PermissionRequest entity
            modelBuilder.Entity<PermissionRequest>(entity =>
            {
                entity.HasKey(pr => pr.Id);
                entity.Property(pr => pr.FeatureCode).HasMaxLength(100).IsRequired();
                entity.Property(pr => pr.EntityName).HasMaxLength(100).IsRequired();
                entity.Property(pr => pr.EntityId).HasMaxLength(255).IsRequired();
                entity.Property(pr => pr.EntityTitle).HasMaxLength(500);
                entity.Property(pr => pr.RequestedAction).HasMaxLength(50).IsRequired();
                entity.Property(pr => pr.Reason).HasMaxLength(1000);
                entity.Property(pr => pr.Status).HasMaxLength(50).IsRequired();
                entity.Property(pr => pr.ReviewNote).HasMaxLength(1000);

                entity.HasOne(pr => pr.User)
                    .WithMany()
                    .HasForeignKey(pr => pr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(pr => pr.Reviewer)
                    .WithMany()
                    .HasForeignKey(pr => pr.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(pr => pr.DuAn)
                    .WithMany()
                    .HasForeignKey(pr => pr.DuAnId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(pr => pr.Permission)
                    .WithMany()
                    .HasForeignKey(pr => pr.PermissionId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(pr => pr.RequestedPermission)
                    .WithMany()
                    .HasForeignKey(pr => pr.RequestedPermissionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => new { e.Id, e.Timestamp });
                entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TableName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EntityId).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.UserId).HasMaxLength(255);
                entity.Property(e => e.IpAddress).HasMaxLength(100);

                entity.HasIndex(e => new { e.Username, e.Timestamp })
                    .HasDatabaseName("IX_AuditLog_Username_Timestamp");
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Content).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Link).HasMaxLength(500);
                entity.Property(e => e.FeatureCode).HasMaxLength(100).HasDefaultValue(string.Empty);
                entity.Property(e => e.EntityName).HasMaxLength(100);
                entity.Property(e => e.EntityId).HasMaxLength(255);
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure CommentCongViecGoiThau entity
            modelBuilder.Entity<CommentCongViecGoiThau>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Content).HasMaxLength(4000).IsRequired();
                
                entity.HasOne(c => c.CongViecGoiThau)
                    .WithMany()
                    .HasForeignKey(c => c.CongViecGoiThauId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CommentMention entity
            modelBuilder.Entity<CommentMention>(entity =>
            {
                entity.HasKey(m => m.Id);

                entity.HasOne(m => m.Comment)
                    .WithMany(c => c.Mentions)
                    .HasForeignKey(m => m.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(m => m.MentionedUser)
                    .WithMany()
                    .HasForeignKey(m => m.MentionedUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Catalog entities
            modelBuilder.Entity<XuatXu>(entity => ConfigureBaseEntity(entity));
            modelBuilder.Entity<DonViTinh>(entity => ConfigureBaseEntity(entity));
            modelBuilder.Entity<HangSanXuat>(entity => ConfigureBaseEntity(entity));

            // Configure HangHoaDichVu entity
            modelBuilder.Entity<HangHoaDichVu>(entity =>
            {
                ConfigureBaseEntity(entity, uniqueCode: false);

                entity.Property(h => h.DonGia)
                    .HasColumnType("decimal(18,2)");

                entity.Property(h => h.ThanhTien)
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(h => new { h.Loai, h.Code })
                    .HasFilter("\"IsDeleted\" = false")
                    .IsUnique();

                entity.HasOne(h => h.XuatXu)
                    .WithMany()
                    .HasForeignKey(h => h.IdXuatXu)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(h => h.HangSanXuat)
                    .WithMany()
                    .HasForeignKey(h => h.IdHangSanXuat)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(h => h.License)
                    .WithMany()
                    .HasForeignKey(h => h.IdLicense)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(h => h.DonViTinh)
                    .WithMany()
                    .HasForeignKey(h => h.IdDonViTinh)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<HopDong>()
                    .WithMany(h => h.HangHoaDichVus)
                    .HasForeignKey(h => h.IdParent)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.PhuLucHopDong)
                    .WithMany(p => p.HangHoaDichVus)
                    .HasForeignKey(h => h.PhuLucHopDongId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure FileAttachment entity
            modelBuilder.Entity<FileAttachment>(entity =>
            {
                entity.HasKey(fa => fa.Id);
                entity.Property(fa => fa.FileName).HasMaxLength(255).IsRequired();
                entity.Property(fa => fa.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(fa => fa.ContentType).HasMaxLength(100);
                entity.Property(fa => fa.EntityType).HasMaxLength(100).IsRequired();
                entity.HasIndex(fa => new { fa.EntityType, fa.EntityId })
                    .HasDatabaseName("IX_FileAttachment_EntityType_EntityId");
            });

            // Configure FileVersion entity
            modelBuilder.Entity<FileVersion>(entity =>
            {
                entity.HasKey(fv => fv.Id);
                entity.Property(fv => fv.FileName).HasMaxLength(255).IsRequired();
                entity.Property(fv => fv.FilePath).HasMaxLength(500).IsRequired();
                entity.Property(fv => fv.ContentType).HasMaxLength(100);
                entity.HasOne(fv => fv.FileAttachment)
                    .WithMany(fa => fa.Versions)
                    .HasForeignKey(fv => fv.FileAttachmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(fv => new { fv.FileAttachmentId, fv.VersionNumber })
                    .IsUnique()
                    .HasDatabaseName("IX_FileVersion_AttachmentId_VersionNumber");
            });
        }

        private static void ConfigureBaseEntity<TEntity>(EntityTypeBuilder<TEntity> builder, bool uniqueCode = true)
            where TEntity : BaseEntity
        {
            builder.HasKey(e => e.Id);
            builder.Property(entity => entity.Code)
                .HasMaxLength(50)
                .IsRequired();
            builder.Property(entity => entity.Name)
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(entity => entity.Description)
                .HasMaxLength(1000);
            
            builder.HasQueryFilter(entity => !entity.IsDeleted);

            if (uniqueCode)
            {
                builder.HasIndex(entity => entity.Code)
                    .HasFilter("\"IsDeleted\" = false")
                    .IsUnique();
            }

            // Composite index tối ưu cho tất cả pagination queries (ORDER BY CreatedAt DESC, Id DESC)
            builder.HasIndex(entity => new { entity.CreatedAt, entity.Id })
                .IsDescending(true, true)
                .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedAt_Id_Desc");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUserService?.GetUserId();
            Guid? resolvedUserId = (userId.HasValue && userId.Value != System.Guid.Empty) ? userId.Value : null;

            if (!resolvedUserId.HasValue)
            {
                var username = _currentUserService?.GetUsername();
                if (!string.IsNullOrEmpty(username))
                {
                    var user = await Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                    if (user != null)
                    {
                        resolvedUserId = user.Id;
                    }
                }
            }

            if (resolvedUserId.HasValue)
            {
                foreach (var entry in ChangeTracker.Entries<CongViecGoiThau>())
                {
                    if (entry.State == EntityState.Added)
                    {
                        entry.Entity.CreateUserId = resolvedUserId.Value;
                        entry.Entity.ModifiedUserId = resolvedUserId.Value;
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        entry.Entity.ModifiedUserId = resolvedUserId.Value;
                    }
                }
            }
            // Detect UserPermission changes and load all descriptions!
            var userPermissionLogs = await PrepareUserPermissionAuditLogsAsync(cancellationToken);
            if (userPermissionLogs.Any())
            {
                AuditLogs.AddRange(userPermissionLogs);
            }

            // Detect CongViecNguoiLienQuan changes and load all descriptions!
            var stakeholderLogs = await PrepareCongViecNguoiLienQuanAuditLogsAsync(cancellationToken);
            if (stakeholderLogs.Any())
            {
                AuditLogs.AddRange(stakeholderLogs);
            }

            var auditEntries = OnBeforeSaveChanges();

            // Translate all added AuditLog actions to Vietnamese
            foreach (var entry in ChangeTracker.Entries<AuditLog>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.Action = TranslateActionName(entry.Entity.Action);
                }
            }

            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChangesAsync(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var username = _currentUserService?.GetUsername() ?? "System/BackgroundJob";
            var ipAddress = _currentUserService?.GetIpAddress();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.Entity is UserPermission || entry.Entity is CongViecNguoiLienQuan || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Metadata.Name,
                    Username = username,
                    IpAddress = ipAddress
                };
                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue ?? "";
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.Action = "CREATE";
                            auditEntry.NewValues[TranslateColumnName(propertyName)] = property.CurrentValue ?? "";
                            break;

                        case EntityState.Deleted:
                            auditEntry.Action = "DELETE";
                            auditEntry.OldValues[TranslateColumnName(propertyName)] = property.OriginalValue ?? "";
                            break;

                        case EntityState.Modified:
                            if (IgnoredAuditProperties.Contains(propertyName))
                                break;
                            if (property.IsModified)
                            {
                                if (!Equals(property.OriginalValue, property.CurrentValue))
                                {
                                    auditEntry.Action = "UPDATE";
                                    var displayName = TranslateColumnName(propertyName);
                                    auditEntry.ChangedColumns.Add(displayName);
                                    auditEntry.OldValues[displayName] = property.OriginalValue ?? "";
                                    auditEntry.NewValues[displayName] = property.CurrentValue ?? "";
                                }
                            }
                            break;
                    }
                }
            }

            foreach (var auditEntry in auditEntries)
            {
                var actionText = FormatActionName(auditEntry.Action);
                var recordName = GetRecordName(auditEntry.Entry);
                auditEntry.Description = $"{auditEntry.Username} {actionText} {recordName}".Trim();
            }

            foreach (var auditEntry in auditEntries.Where(ae => !ae.Entry.Properties.Any(p => p.Metadata.IsPrimaryKey() && p.IsTemporary)))
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            return auditEntries.Where(ae => ae.Entry.Properties.Any(p => p.Metadata.IsPrimaryKey() && p.IsTemporary)).ToList();
        }

        private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                foreach (var prop in auditEntry.Entry.Properties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue ?? "";
                    }
                }

                var actionText = FormatActionName(auditEntry.Action);
                var recordName = GetRecordName(auditEntry.Entry);
                auditEntry.Description = $"{auditEntry.Username} {actionText} {recordName}".Trim();

                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            // Translate all added AuditLog actions to Vietnamese
            foreach (var entry in ChangeTracker.Entries<AuditLog>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.Action = TranslateActionName(entry.Entity.Action);
                }
            }

            await base.SaveChangesAsync();
        }

        private async Task<List<AuditLog>> PrepareUserPermissionAuditLogsAsync(CancellationToken cancellationToken)
        {
            var auditLogs = new List<AuditLog>();
            var entries = ChangeTracker.Entries<UserPermission>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                .ToList();

            if (!entries.Any())
                return auditLogs;

            var username = _currentUserService?.GetUsername() ?? "System/BackgroundJob";
            var ipAddress = _currentUserService?.GetIpAddress();
            var userId = _currentUserService?.GetUserId();
            Guid? resolvedUserId = (userId.HasValue && userId.Value != System.Guid.Empty) ? userId.Value : null;

            if (!resolvedUserId.HasValue && !string.IsNullOrEmpty(username))
            {
                var user = await Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                if (user != null)
                {
                    resolvedUserId = user.Id;
                }
            }

            foreach (var entry in entries)
            {
                var userPerm = entry.Entity;
                var state = entry.State;

                // 1. Get recipient user info
                var recipient = await Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userPerm.UserId, cancellationToken);
                var recipientName = recipient != null ? $"{recipient.Username} ({recipient.FullName})" : userPerm.UserId.ToString();

                // 2. Get permission info
                var perm = await Permissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == userPerm.PermissionId, cancellationToken);
                var permName = perm != null ? perm.Name : userPerm.PermissionId.ToString();

                // 3. Get target description
                string targetDesc = "";
                string targetTableName = "UserPermissions";
                string targetEntityId = userPerm.Id.ToString();
                Guid? targetGuid = Guid.TryParse(userPerm.EntityId, out var parsedId) ? parsedId : null;

                if (!string.IsNullOrEmpty(userPerm.EntityName))
                {
                    var normEntity = userPerm.EntityName.ToLowerInvariant();

                    if (targetGuid.HasValue)
                    {
                        if (normEntity == "duan" || normEntity == "project")
                        {
                            targetTableName = "DuAns";
                            targetEntityId = targetGuid.Value.ToString();
                            var p = await DuAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (p != null) targetDesc = $"dự án '{p.Name}'";
                        }
                        else if (normEntity == "hopdong" || normEntity == "contract")
                        {
                            targetTableName = "HopDongs";
                            targetEntityId = targetGuid.Value.ToString();
                            var h = await HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (h != null) targetDesc = $"hợp đồng '{h.Name ?? h.Code}'";
                        }
                        else if (normEntity == "goithau" || normEntity == "package")
                        {
                            targetTableName = "GoiThaus";
                            targetEntityId = targetGuid.Value.ToString();
                            var gt = await GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (gt != null) targetDesc = $"gói thầu '{gt.Name}'";
                        }
                        else if (normEntity == "license")
                        {
                            targetTableName = "Licenses";
                            targetEntityId = targetGuid.Value.ToString();
                            var l = await Licenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (l != null) targetDesc = $"license '{l.Name}'";
                        }
                        else if (normEntity == "doitac" || normEntity == "partner")
                        {
                            targetTableName = "DoiTacs";
                            targetEntityId = targetGuid.Value.ToString();
                            var dt = await DoiTacs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (dt != null) targetDesc = $"đối tác '{dt.Name}'";
                        }
                        else if (normEntity == "congviecgoithau" || normEntity == "cong_viec" || normEntity == "task")
                        {
                            targetTableName = "CongViecGoiThaus";
                            targetEntityId = targetGuid.Value.ToString();
                            var cv = await CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                            if (cv != null) targetDesc = $"công việc '{cv.TenTaiLieu}'";
                        }
                    }

                    if (string.IsNullOrEmpty(targetDesc) && userPerm.DuAnId.HasValue)
                    {
                        var p = await DuAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userPerm.DuAnId.Value, cancellationToken);
                        if (p != null)
                        {
                            string featName = normEntity switch
                            {
                                "goithau" or "package" => "các gói thầu",
                                "hopdong" or "contract" => "các hợp đồng",
                                "congviec" or "task" or "cong_viec" => "các công việc",
                                _ => userPerm.EntityName
                            };
                            targetDesc = $"{featName} thuộc dự án '{p.Name}'";
                            targetTableName = "DuAns";
                            targetEntityId = userPerm.DuAnId.Value.ToString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(targetDesc))
                {
                    if (userPerm.DuAnId.HasValue)
                    {
                        targetTableName = "DuAns";
                        targetEntityId = userPerm.DuAnId.Value.ToString();
                        var p = await DuAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userPerm.DuAnId.Value, cancellationToken);
                        if (p != null) targetDesc = $"dự án '{p.Name}'";
                    }
                    else
                    {
                        targetDesc = $"{userPerm.EntityName} {userPerm.EntityId}";
                    }
                }

                string action = state == EntityState.Added ? "GRANT_PERMISSION" : "REVOKE_PERMISSION";
                string actionDesc = state == EntityState.Added ? "cấp quyền" : "thu hồi quyền";
                string relationWord = state == EntityState.Added ? "cho" : "của";
                string description = $"{username} {actionDesc} {permName} {relationWord} người dùng {recipientName} trên {targetDesc}";

                var log = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = resolvedUserId?.ToString(),
                    Username = username,
                    Action = action,
                    TableName = targetTableName,
                    EntityId = targetEntityId,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Description = description
                };

                string targetName = userPerm.EntityId;
                string projectName = "";
                
                if (targetGuid.HasValue && !string.IsNullOrEmpty(userPerm.EntityName))
                {
                    var normEntity = userPerm.EntityName.ToLowerInvariant();
                    if (normEntity == "duan" || normEntity == "project")
                    {
                        var p = await DuAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (p != null) targetName = p.Name;
                    }
                    else if (normEntity == "hopdong" || normEntity == "contract")
                    {
                        var h = await HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (h != null) targetName = h.Name ?? h.Code;
                    }
                    else if (normEntity == "goithau" || normEntity == "package")
                    {
                        var gt = await GoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (gt != null) targetName = gt.Name;
                    }
                    else if (normEntity == "license")
                    {
                        var l = await Licenses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (l != null) targetName = l.Name;
                    }
                    else if (normEntity == "doitac" || normEntity == "partner")
                    {
                        var dt = await DoiTacs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (dt != null) targetName = dt.Name;
                    }
                    else if (normEntity == "congviecgoithau" || normEntity == "cong_viec" || normEntity == "task")
                    {
                        var cv = await CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == targetGuid.Value, cancellationToken);
                        if (cv != null) targetName = cv.TenTaiLieu;
                    }
                }

                if (userPerm.DuAnId.HasValue)
                {
                    var p = await DuAns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userPerm.DuAnId.Value, cancellationToken);
                    if (p != null) projectName = p.Name;
                }

                string vFeatureCode = userPerm.FeatureCode;
                if (vFeatureCode == "DU_AN") vFeatureCode = "Dự án";
                else if (vFeatureCode == "HOP_DONG") vFeatureCode = "Hợp đồng";
                else if (vFeatureCode == "GOI_THAU") vFeatureCode = "Gói thầu";
                else if (vFeatureCode == "DOI_TAC") vFeatureCode = "Đối tác";
                else if (vFeatureCode == "LICENSE") vFeatureCode = "License";

                var payload = new Dictionary<string, object?>
                {
                    { "Tên người dùng", recipient != null ? recipient.FullName : userPerm.UserId.ToString() },
                    { "Mã người dùng", userPerm.UserId },
                    { "Quyền", permName },
                    { "Mã quyền", userPerm.PermissionId },
                    { "Chức năng", vFeatureCode },
                    { "Tên loại đối tượng", userPerm.EntityName == "DuAn" ? "Dự án" : userPerm.EntityName },
                    { "Đối tượng", targetName },
                    { "Mã đối tượng", userPerm.EntityId },
                    { "Dự án", projectName },
                    { "Mã dự án", userPerm.DuAnId }
                };

                var serializedVals = System.Text.Json.JsonSerializer.Serialize(payload);

                if (state == EntityState.Added)
                {
                    log.NewValues = serializedVals;
                }
                else
                {
                    log.OldValues = serializedVals;
                }

                auditLogs.Add(log);
            }

            return auditLogs;
        }

        private async Task<List<AuditLog>> PrepareCongViecNguoiLienQuanAuditLogsAsync(CancellationToken cancellationToken)
        {
            var auditLogs = new List<AuditLog>();
            var entries = ChangeTracker.Entries<CongViecNguoiLienQuan>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                .ToList();

            if (!entries.Any())
                return auditLogs;

            var username = _currentUserService?.GetUsername() ?? "System/BackgroundJob";
            var ipAddress = _currentUserService?.GetIpAddress();
            var userId = _currentUserService?.GetUserId();
            Guid? resolvedUserId = (userId.HasValue && userId.Value != System.Guid.Empty) ? userId.Value : null;

            if (!resolvedUserId.HasValue && !string.IsNullOrEmpty(username))
            {
                var user = await Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                if (user != null)
                {
                    resolvedUserId = user.Id;
                }
            }

            foreach (var entry in entries)
            {
                var record = entry.Entity;
                var state = entry.State;

                // 1. Get task details
                var task = await CongViecGoiThaus.AsNoTracking().FirstOrDefaultAsync(t => t.Id == record.CongViecGoiThauId, cancellationToken);
                
                string projectName = "Dự án";
                string targetTableName = "CongViecGoiThaus";
                string targetEntityId = record.CongViecGoiThauId.ToString();

                if (task != null && task.GoiThauId != Guid.Empty)
                {
                    var goiThau = await GoiThaus.AsNoTracking().FirstOrDefaultAsync(g => g.Id == task.GoiThauId, cancellationToken);
                    if (goiThau != null && goiThau.DuAnId.HasValue)
                    {
                        var duAn = await DuAns.AsNoTracking().FirstOrDefaultAsync(d => d.Id == goiThau.DuAnId.Value, cancellationToken);
                        if (duAn != null)
                        {
                            projectName = duAn.Name;
                            targetTableName = "DuAns";
                            targetEntityId = duAn.Id.ToString();
                        }
                    }
                }

                string action = state == EntityState.Added ? "ADD_STAKEHOLDER" : "REMOVE_STAKEHOLDER";
                string actionDesc = state == EntityState.Added ? "thêm người liên quan vào" : "xóa người liên quan khỏi";
                string description = $"{username} {actionDesc} {projectName}";

                var log = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = resolvedUserId?.ToString(),
                    Username = username,
                    Action = action,
                    TableName = targetTableName,
                    EntityId = targetEntityId,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    Description = description
                };

                var stakeholder = await Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == record.UserId, cancellationToken);
                var payload = new Dictionary<string, object?>
                {
                    { "Tên người dùng", stakeholder != null ? stakeholder.FullName : record.UserId.ToString() },
                    { "Mã người dùng", record.UserId },
                    { "Công việc", task != null ? task.TenTaiLieu : record.CongViecGoiThauId.ToString() },
                    { "Mã công việc", record.CongViecGoiThauId },
                    { "Trạng thái xác nhận", record.TrangThaiXacNhan }
                };

                var serializedVals = System.Text.Json.JsonSerializer.Serialize(payload);

                if (state == EntityState.Added)
                {
                    log.NewValues = serializedVals;
                }
                else
                {
                    log.OldValues = serializedVals;
                }

                auditLogs.Add(log);
            }

            return auditLogs;
        }

        private static string GetRecordName(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var properties = entry.Properties.ToList();

            // 1. Check for "Name" property
            var nameProp = properties.FirstOrDefault(p => p.Metadata.Name.Equals("Name", StringComparison.OrdinalIgnoreCase));
            if (nameProp != null)
            {
                var val = (nameProp.CurrentValue ?? nameProp.OriginalValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            // 2. Check for "Ten..." property (e.g. TenDuAn, TenHopDong, TenGoiThau, TenPhongBan, TenDotThanhToan)
            var tenProp = properties.FirstOrDefault(p => p.Metadata.Name.StartsWith("Ten", StringComparison.OrdinalIgnoreCase));
            if (tenProp != null)
            {
                var val = (tenProp.CurrentValue ?? tenProp.OriginalValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            // 3. Check for Title, Code, Username, FileName, FullName
            var candidateProp = properties.FirstOrDefault(p =>
                p.Metadata.Name.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
                p.Metadata.Name.Equals("Code", StringComparison.OrdinalIgnoreCase) ||
                p.Metadata.Name.Equals("Username", StringComparison.OrdinalIgnoreCase) ||
                p.Metadata.Name.Equals("FileName", StringComparison.OrdinalIgnoreCase) ||
                p.Metadata.Name.Equals("FullName", StringComparison.OrdinalIgnoreCase));

            if (candidateProp != null)
            {
                var val = (candidateProp.CurrentValue ?? candidateProp.OriginalValue)?.ToString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }

            // 4. Fallback to Primary Key or entity type name
            var keyProp = properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            var keyVal = (keyProp?.CurrentValue ?? keyProp?.OriginalValue)?.ToString();
            if (!string.IsNullOrWhiteSpace(keyVal))
            {
                return keyVal;
            }

            return entry.Metadata.ClrType.Name;
        }

        private static readonly Dictionary<string, string> ColumnNameTranslations = new(StringComparer.OrdinalIgnoreCase)
        {
            // Common / Base Entity
            { "Id", "ID" },
            { "Code", "Mã/Số" },
            { "Name", "Tên" },
            { "Description", "Mô tả" },
            { "CreatedAt", "Ngày tạo" },
            { "CreatedBy", "Người tạo" },
            { "UpdatedAt", "Ngày cập nhật" },
            { "UpdatedBy", "Người cập nhật" },
            { "IsDeleted", "Trạng thái xóa" },
            { "DeletedAt", "Thời gian xóa" },
            { "DeletedByUserId", "Người xóa" },

            // User / Account
            { "Username", "Tên đăng nhập" },
            { "FullName", "Họ và tên" },
            { "PasswordHash", "Mật khẩu mã hóa" },
            { "Email", "Email" },
            { "Phone", "Số điện thoại" },
            { "IsActive", "Trạng thái hoạt động" },
            { "IsSystemAdmin", "Quản trị hệ thống" },
            { "IdChucVu", "Chức vụ" },
            { "IdPhongBan", "Phòng ban" },
            { "IdDonVi", "Đơn vị" },
            { "IdToNhom", "Tổ nhóm" },
            { "CanViewHopDong", "Quyền xem hợp đồng" },

            // DuAn (Project)
            { "DuToanPheDuyet", "Dự toán phê duyệt" },
            { "NhomDuAnId", "Nhóm dự án" },
            { "PhanLoaiDuAnId", "Phân loại dự án" },
            { "NguonVonId", "Nguồn vốn" },
            { "ChuDauTu", "Chủ đầu tư" },
            { "DiaDiemThucHien", "Địa điểm thực hiện" },
            { "ThoiGianThucHien", "Thời gian thực hiện" },
            { "NguonDuAnIds", "Nguồn vốn dự án" },
            { "NoiDung", "Nội dung dự án" },
            { "ToChucThucHien", "Tổ chức thực hiện" },
            { "CreatedByUserId", "Người tạo dự án" },
            { "ChuDuAnId", "Chủ dự án" },
            { "ProjectManagerId", "Quản lý dự án" },
            { "HinhThucQuanLyId", "Hình thức quản lý" },
            { "LoaiDuAn", "Loại dự án" },

            // HopDong (Contract)
            { "GiaTriHopDong", "Giá trị hợp đồng" },
            { "ThoiHanThucHien", "Thời hạn thực hiện" },
            { "GoiThauId", "Gói thầu" },
            { "ChuDauTuId", "Chủ đầu tư" },
            { "NhaThauId", "Nhà thầu" },
            { "DuAnId", "Dự án" },
            { "LoaiHopDongId", "Loại hợp đồng" },
            { "NgayKy", "Ngày ký" },
            { "NgayHieuLuc", "Ngày hiệu lực" },
            { "NgayKetThuc", "Ngày kết thúc" },
            { "ExpiredDate", "Ngày hết hạn" },
            { "HinhThucThanhToan", "Hình thức thanh toán" },
            { "IsRenewalRequired", "Yêu cầu gia hạn" },
            { "LoaiHopDong", "Phân loại hợp đồng" },
            { "RenewalReminderDate", "Ngày nhắc gia hạn" },
            { "GiaHopDong", "Giá hợp đồng" },
            { "SoHopDong", "Số hợp đồng" },
            { "TenHopDong", "Tên hợp đồng" },

            // GoiThau (Package)
            { "GiaTriGoiThau", "Giá trị gói thầu" },
            { "HinhThucLuaChon", "Hình thức lựa chọn nhà thầu" },
            { "PhuongThucLuaChon", "Phương thức lựa chọn nhà thầu" },
            { "ThoiGianBatDau", "Thời gian bắt đầu" },
            { "ThoiGianKetThuc", "Thời gian kết thúc" },
            { "TenGoiThau", "Tên gói thầu" },
            { "MaGoiThau", "Mã gói thầu" },

            // DieuChinhDuAn (Project Adjustment)
            { "LyDoDieuChinh", "Lý do điều chỉnh" },
            { "GiaTriTruocDieuChinh", "Giá trị trước điều chỉnh" },
            { "GiaTriSauDieuChinh", "Giá trị sau điều chỉnh" },

            // DotThanhToan (Payment)
            { "TenDot", "Tên đợt thanh toán" },
            { "TyLeThanhToan", "Tỷ lệ thanh toán (%)" },
            { "GiaTriThanhToan", "Giá trị thanh toán" },
            { "HopDongId", "Hợp đồng" },
            { "NgayThanhToan", "Ngày thanh toán" },
            { "TrangThai", "Trạng thái thanh toán" },
            { "GhiChu", "Ghi chú" },

            // CongViecGoiThau (Task)
            { "TenTaiLieu", "Tên công việc/tài liệu" },
            { "LoaiVanBan", "Loại văn bản" },
            { "TinhTrang", "Tình trạng" },
            { "CreateUserId", "Người tạo công việc" },
            { "ModifiedUserId", "Người cập nhật công việc" },
            { "Status", "Trạng thái" },
            { "MucDoUuTien", "Mức độ ưu tiên" },
            { "NgayBatDau", "Ngày bắt đầu" },

            // DoiTac (Partner / Contractor)
            { "TaxCode", "Mã số thuế" },
            { "Address", "Địa chỉ" },
            { "Account", "Tài khoản ngân hàng" },
            { "Representative", "Người đại diện" },
            { "Position", "Chức vụ" },

            // License
            { "ThongTinThietBi", "Thông tin thiết bị" },
            { "NhaCungCapId", "Nhà cung cấp" },
            { "NgayKichHoat", "Ngày kích hoạt" },
            { "NgayHetHan", "Ngày hết hạn" },

            // Misc / Financial
            { "TienTe", "Tiền tệ" },
            { "TyGia", "Tỷ giá" },
            { "TriGia", "Trị giá" }
        };

        public static string TranslateColumnName(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return columnName;
            return ColumnNameTranslations.TryGetValue(columnName, out var translation) ? translation : columnName;
        }

        public static string TranslateActionName(string action)
        {
            if (string.IsNullOrEmpty(action)) return action;
            return action.ToUpperInvariant() switch
            {
                "CREATE" => "Tạo mới",
                "UPDATE" => "Cập nhật",
                "DELETE" => "Xóa",
                "ACCESS_GRANTED" => "Truy cập thành công",
                "ACCESS_DENIED" => "Bị từ chối truy cập",
                "GRANT_PERMISSION" => "Cấp quyền",
                "REVOKE_PERMISSION" => "Thu hồi quyền",
                "ADD_STAKEHOLDER" => "Thêm người liên quan",
                "REMOVE_STAKEHOLDER" => "Xóa người liên quan",
                _ => action
            };
        }

        private static string FormatActionName(string action)
        {
            return action.ToUpperInvariant() switch
            {
                "CREATE" => "tạo mới",
                "UPDATE" => "cập nhật",
                "DELETE" => "xóa",
                _ => action.ToLowerInvariant()
            };
        }
    }
}
