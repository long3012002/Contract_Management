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
            modelBuilder.Entity<ContractPartner>()
                .Property(contractPartner => contractPartner.Role)
                .HasMaxLength(50);
            modelBuilder.Entity<ContractPartner>()
                .HasOne<Contract>()
                .WithMany()
                .HasForeignKey(contractPartner => contractPartner.ContractId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ContractPartner>()
                .HasOne<Partner>()
                .WithMany()
                .HasForeignKey(contractPartner => contractPartner.PartnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureBaseEntity<TEntity>(EntityTypeBuilder<TEntity> builder)
            where TEntity : BaseEntity
        {
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
