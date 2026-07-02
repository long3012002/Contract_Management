using demo1.DTOs;
using demo1.Entity;
using demo1.Mapper;
using demo1.Services.Interfaces;
using demo1.Validator;

namespace demo1.Services.Implements;

public class BidPackageService : InMemoryCrudService<BidPackage, BidPackageDto, CreateBidPackageDto, UpdateBidPackageDto>, IBidPackageService
{
    public BidPackageService()
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

    protected override BidPackageDto ToDto(BidPackage entity) => BidPackageMapper.ToDto(entity);

    protected override BidPackage CreateEntity(CreateBidPackageDto dto)
    {
        BidPackageValidator.EnsureValid(dto.EstimatedValue, dto.WarningThresholdPercent);
        return BidPackageMapper.ToEntity(dto);
    }

    protected override void UpdateEntity(BidPackage entity, UpdateBidPackageDto dto)
    {
        BidPackageValidator.EnsureValid(dto.EstimatedValue, dto.WarningThresholdPercent);
        BidPackageMapper.ApplyUpdate(entity, dto);
    }
}
