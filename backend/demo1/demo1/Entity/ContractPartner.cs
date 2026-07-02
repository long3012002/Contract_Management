namespace demo1.Entity;

public class ContractPartner
{
    public int ContractId { get; set; }
    public int PartnerId { get; set; }
    public string Role { get; set; } = "Primary";
}
