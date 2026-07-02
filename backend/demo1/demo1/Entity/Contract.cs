namespace demo1.Entity;

public class Contract : BaseEntity
{
    public int? ProjectId { get; set; }
    public int? BidPackageId { get; set; }
    public decimal ContractValue { get; set; }
    public DateTime? SignedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiredDate { get; set; }
    public DateTime? RenewalReminderDate { get; set; }
    public bool IsRenewalRequired { get; set; } = true;
    public string Status { get; set; } = "Draft";
}
