using demo1.DTOs;
using demo1.Entity;
using demo1.Services.Interfaces;
using demo1.Validator;
using AutoMapper;
using demo1.Data;

namespace demo1.Services.Implements;

public class BidPackageService : DbCrudService<BidPackage, BidPackageDto, CreateBidPackageDto, UpdateBidPackageDto>, IBidPackageService
{
    public BidPackageService(AppDbContext dbContext, IMapper mapper) : base(dbContext, mapper)
    {
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
