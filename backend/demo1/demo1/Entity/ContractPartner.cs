namespace demo1.Entity;

public class ContractPartner
{
    public Guid ContractId { get; set; }
    public Guid PartnerId { get; set; }
    public string Role { get; set; } = "Primary";
}
