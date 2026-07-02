using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;

namespace demo1.Services.Implements;

public class BidPackageService : InMemoryCrudService<BidPackage, BidPackageDto, CreateBidPackageDto, UpdateBidPackageDto>, IBidPackageService
{
    public BidPackageService(IMapper mapper) : base(mapper)
    {
        Seed(new BidPackage
        {
            Code = "GT-001",
            Name = "Goi thau trien khai phan mem",
            ProjectId = 1,
            EstimatedValue = 800_000_000,
            WarningThresholdPercent = 100
        });
    }

    protected override BidPackage CreateEntity(CreateBidPackageDto dto)
    {
        BidPackageValidator.EnsureValid(dto.EstimatedValue, dto.WarningThresholdPercent);
        return base.CreateEntity(dto);
    }

    protected override void UpdateEntity(BidPackage entity, UpdateBidPackageDto dto)
    {
        BidPackageValidator.EnsureValid(dto.EstimatedValue, dto.WarningThresholdPercent);
        base.UpdateEntity(entity, dto);
    }
}
