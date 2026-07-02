namespace demo1.Entity;

public class Resolution : BaseEntity
{
    public DateTime? IssuedDate { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? FileUrl { get; set; }
}
