using demo1.Entity;
using Microsoft.EntityFrameworkCore;

namespace demo1.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var now = DateTime.UtcNow;
        var projectId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bidPackageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var partnerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var contractId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var resolutionId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        if (!await context.Projects.AnyAsync(project => project.Code == "PRJ-DEMO-001"))
        {
            await context.Projects.AddAsync(new Project
            {
                Id = projectId,
                Code = "PRJ-DEMO-001",
                Name = "Demo project",
                Description = "Sample project for frontend integration.",
                TotalBudget = 500000000,
                Status = "Active",
                CreatedAt = now
            });
        }

        if (!await context.BidPackages.AnyAsync(bidPackage => bidPackage.Code == "BID-DEMO-001"))
        {
            await context.BidPackages.AddAsync(new BidPackage
            {
                Id = bidPackageId,
                ProjectId = projectId,
                Code = "BID-DEMO-001",
                Name = "Demo bid package",
                Description = "Sample bid package for API testing.",
                EstimatedValue = 300000000,
                WarningThresholdPercent = 90,
                CreatedAt = now
            });
        }

        if (!await context.Partners.AnyAsync(partner => partner.Code == "PAR-DEMO-001"))
        {
            await context.Partners.AddAsync(new Partner
            {
                Id = partnerId,
                Code = "PAR-DEMO-001",
                Name = "Demo partner",
                Description = "Sample partner for API testing.",
                TaxCode = "0123456789",
                Phone = "0900000000",
                Email = "partner@example.com",
                Address = "Demo address",
                CreatedAt = now
            });
        }

        if (!await context.Contracts.AnyAsync(contract => contract.Code == "CTR-DEMO-001"))
        {
            await context.Contracts.AddAsync(new Contract
            {
                Id = contractId,
                ProjectId = projectId,
                BidPackageId = bidPackageId,
                Code = "CTR-DEMO-001",
                Name = "Demo contract",
                Description = "Sample contract for frontend screens.",
                ContractValue = 280000000,
                SignedDate = now.Date.AddDays(-10),
                EffectiveDate = now.Date.AddDays(-5),
                ExpiredDate = now.Date.AddDays(25),
                RenewalReminderDate = now.Date.AddDays(10),
                IsRenewalRequired = true,
                Status = "Active",
                CreatedAt = now
            });
        }

        if (!await context.Resolutions.AnyAsync(resolution => resolution.Code == "RES-DEMO-001"))
        {
            await context.Resolutions.AddAsync(new Resolution
            {
                Id = resolutionId,
                Code = "RES-DEMO-001",
                Name = "Demo resolution",
                Description = "Sample resolution for API testing.",
                IssuedDate = now.Date.AddDays(-20),
                EffectiveDate = now.Date.AddDays(-15),
                FileUrl = "https://example.com/demo-resolution.pdf",
                CreatedAt = now
            });
        }

        if (!await context.ContractPartners.AnyAsync(contractPartner =>
                contractPartner.ContractId == contractId && contractPartner.PartnerId == partnerId))
        {
            await context.ContractPartners.AddAsync(new ContractPartner
            {
                ContractId = contractId,
                PartnerId = partnerId,
                Role = "Primary"
            });
        }

        await context.SaveChangesAsync();
    }
}
