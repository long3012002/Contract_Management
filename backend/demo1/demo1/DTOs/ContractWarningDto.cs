namespace demo1.DTOs;

public class ContractWarningDto
{
    public Guid ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime? ExpiredDate { get; set; }
    public int DaysRemaining { get; set; }
    public string WarningMessage { get; set; } = string.Empty;
}
