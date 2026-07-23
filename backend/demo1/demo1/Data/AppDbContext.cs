using Microsoft.EntityFrameworkCore;
using demo1.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using demo1.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace demo1.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService? currentUserService = null) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<DuAn> DuAns { get; set; } = null!;
        public DbSet<NhomDuAn> NhomDuAns { get; set; } = null!;
        public DbSet<PhanLoaiDuAn> PhanLoaiDuAns { get; set; } = null!;
        public DbSet<GoiThau> GoiThaus { get; set; } = null!;
        public DbSet<DieuChinhDuAn> DieuChinhDuAns { get; set; } = null!;
        public DbSet<HopDong> HopDongs { get; set; } = null!;
        public DbSet<DoiTac> DoiTacs { get; set; } = null!;
        public DbSet<DotThanhToan> DotThanhToans { get; set; } = null!;
        public DbSet<Resolution> Resolutions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;
        public DbSet<Feature> Features { get; set; } = null!;
        public DbSet<PhongBan> PhongBans { get; set; } = null!;
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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureBaseEntity(modelBuilder.Entity<DuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<NhomDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<PhanLoaiDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<DieuChinhDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<GoiThau>());
            ConfigureBaseEntity(modelBuilder.Entity<HopDong>());
            ConfigureBaseEntity(modelBuilder.Entity<DoiTac>());
            ConfigureBaseEntity(modelBuilder.Entity<Resolution>());
            modelBuilder.Entity<NhaThauGoiThau>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
            ConfigureBaseEntity(modelBuilder.Entity<CongViecGoiThau>());
            ConfigureBaseEntity(modelBuilder.Entity<License>());
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
                .Property(da => da.ChuDauTu)
                .HasMaxLength(255);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.DiaDiemThucHien)
                .HasMaxLength(500);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.ThoiGianThucHien)
                .HasMaxLength(255);
            modelBuilder.Entity<DuAn>()
                .Property(da => da.NguonDuAnIds)
                .HasMaxLength(2000);
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
                .Property(gt => gt.NguongCanhBaoPercent)
                .HasPrecision(5, 2);
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

            modelBuilder.Entity<Resolution>()
                .Property(resolution => resolution.FileUrl)
                .HasMaxLength(1000);

            // Configure composite key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

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
            });

            // Configure Notification entity
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Content).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.Link).HasMaxLength(500);
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
        }

        private static void ConfigureBaseEntity<TEntity>(EntityTypeBuilder<TEntity> builder)
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
            builder.HasIndex(entity => entity.Code)
                .IsUnique();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = OnBeforeSaveChanges();
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
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
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
                            auditEntry.NewValues[propertyName] = property.CurrentValue ?? "";
                            break;

                        case EntityState.Deleted:
                            auditEntry.Action = "DELETE";
                            auditEntry.OldValues[propertyName] = property.OriginalValue ?? "";
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                if (!Equals(property.OriginalValue, property.CurrentValue))
                                {
                                    auditEntry.Action = "UPDATE";
                                    auditEntry.ChangedColumns.Add(propertyName);
                                    auditEntry.OldValues[propertyName] = property.OriginalValue ?? "";
                                    auditEntry.NewValues[propertyName] = property.CurrentValue ?? "";
                                }
                            }
                            break;
                    }
                }
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
                AuditLogs.Add(auditEntry.ToAuditLog());
            }
            await base.SaveChangesAsync();
        }
    }
}
