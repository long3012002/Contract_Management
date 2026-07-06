using Microsoft.EntityFrameworkCore;
using demo1.Entity;

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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for ContractPartner
            modelBuilder.Entity<ContractPartner>()
                .HasKey(cp => new { cp.ContractId, cp.PartnerId });

            // Configure composite key for UserRole
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            // Configure composite key for RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.FeatureId });
        }
    }
}
