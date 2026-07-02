using demo1.DTOs;
using demo1.Entity;

namespace demo1.Mapper;

public static class BidPackageMapper
{
    public static BidPackageDto ToDto(BidPackage entity)
    {
        return new BidPackageDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            ProjectId = entity.ProjectId,
            EstimatedValue = entity.EstimatedValue,
            WarningThresholdPercent = entity.WarningThresholdPercent,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static BidPackage ToEntity(CreateBidPackageDto dto)
    {
        return new BidPackage
        {
            Code = MapperHelpers.NormalizeCode(dto.Code),
            Name = MapperHelpers.TrimRequired(dto.Name),
            Description = MapperHelpers.TrimOptional(dto.Description),
            ProjectId = dto.ProjectId,
            EstimatedValue = dto.EstimatedValue,
            WarningThresholdPercent = dto.WarningThresholdPercent
        };
    }

    public static void ApplyUpdate(BidPackage entity, UpdateBidPackageDto dto)
    {
        entity.Name = MapperHelpers.TrimRequired(dto.Name);
        entity.Description = MapperHelpers.TrimOptional(dto.Description);
        entity.ProjectId = dto.ProjectId;
        entity.EstimatedValue = dto.EstimatedValue;
        entity.WarningThresholdPercent = dto.WarningThresholdPercent;
        entity.IsActive = dto.IsActive;
    }
}
