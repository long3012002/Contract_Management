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
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<PhongBan> PhongBans { get; set; } = null!;
        public DbSet<ChucVu> ChucVus { get; set; } = null!;
        public DbSet<PhongBanPermission> PhongBanPermissions { get; set; } = null!;
        public DbSet<ChucVuPermission> ChucVuPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureBaseEntity(modelBuilder.Entity<DuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<DieuChinhDuAn>());
            ConfigureBaseEntity(modelBuilder.Entity<GoiThau>());
            ConfigureBaseEntity(modelBuilder.Entity<HopDong>());
            ConfigureBaseEntity(modelBuilder.Entity<DoiTac>());
            ConfigureBaseEntity(modelBuilder.Entity<Resolution>());

            modelBuilder.Entity<DuAn>()
                .Property(da => da.DuToanPheDuyet)
                .HasPrecision(18, 2);

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

            // Configure composite key for RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.FeatureId });

            // Configure composite key for PhongBanPermission
            modelBuilder.Entity<PhongBanPermission>()
                .HasKey(pbp => new { pbp.PhongBanId, pbp.FeatureId });

            // Configure composite key for ChucVuPermission
            modelBuilder.Entity<ChucVuPermission>()
                .HasKey(cvp => new { cvp.ChucVuId, cvp.FeatureId });

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
