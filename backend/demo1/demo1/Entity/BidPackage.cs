namespace demo1.Entity;

public class BidPackage : BaseEntity
{
    public int? ProjectId { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal WarningThresholdPercent { get; set; } = 100;
}
