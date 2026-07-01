namespace ContractManagement.Api.DTOs.DocumentTypes;

public class CreateDocumentTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
