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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite key for ContractPartner
            modelBuilder.Entity<ContractPartner>()
                .HasKey(cp => new { cp.ContractId, cp.PartnerId });
        }
    }
}
