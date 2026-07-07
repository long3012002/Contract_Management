using Microsoft.EntityFrameworkCore;
using demo1.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace demo1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<BidPackage> BidPackages { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<Partner> Partners { get; set; } = null!;
        public DbSet<ContractPartner> ContractPartners { get; set; } = null!;
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

            ConfigureBaseEntity(modelBuilder.Entity<Project>());
            ConfigureBaseEntity(modelBuilder.Entity<BidPackage>());
            ConfigureBaseEntity(modelBuilder.Entity<Contract>());
            ConfigureBaseEntity(modelBuilder.Entity<Partner>());
            ConfigureBaseEntity(modelBuilder.Entity<Resolution>());

            modelBuilder.Entity<Project>()
                .Property(project => project.TotalBudget)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Project>()
                .Property(project => project.Status)
                .HasMaxLength(50);

            modelBuilder.Entity<BidPackage>()
                .Property(bidPackage => bidPackage.EstimatedValue)
                .HasPrecision(18, 2);
            modelBuilder.Entity<BidPackage>()
                .Property(bidPackage => bidPackage.WarningThresholdPercent)
                .HasPrecision(5, 2);
            modelBuilder.Entity<BidPackage>()
                .HasOne<Project>()
                .WithMany()
                .HasForeignKey(bidPackage => bidPackage.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Contract>()
                .Property(contract => contract.ContractValue)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Contract>()
                .Property(contract => contract.Status)
                .HasMaxLength(50);
            modelBuilder.Entity<Contract>()
                .HasOne<Project>()
                .WithMany()
                .HasForeignKey(contract => contract.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Contract>()
                .HasOne<BidPackage>()
                .WithMany()
                .HasForeignKey(contract => contract.BidPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Partner>()
                .Property(partner => partner.TaxCode)
                .HasMaxLength(50);
            modelBuilder.Entity<Partner>()
                .Property(partner => partner.Phone)
                .HasMaxLength(30);
            modelBuilder.Entity<Partner>()
                .Property(partner => partner.Email)
                .HasMaxLength(255);
            modelBuilder.Entity<Partner>()
                .Property(partner => partner.Address)
                .HasMaxLength(500);

            modelBuilder.Entity<Resolution>()
                .Property(resolution => resolution.FileUrl)
                .HasMaxLength(1000);

            modelBuilder.Entity<ContractPartner>()
                .HasKey(cp => new { cp.ContractId, cp.PartnerId });

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
    }
}
