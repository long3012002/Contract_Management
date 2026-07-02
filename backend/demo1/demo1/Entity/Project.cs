namespace demo1.Entity;

public class Project : BaseEntity
{
    public decimal TotalBudget { get; set; }
    public string Status { get; set; } = "Planning";
}
