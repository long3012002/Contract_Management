namespace demo1.Entity;

public class BidPackage : BaseEntity
{
    public Guid? ProjectId { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal WarningThresholdPercent { get; set; } = 100;
}
