namespace demo1.DTOs
{
    public class BudgetWarningDto
    {
        public Guid ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public decimal ContractValue { get; set; }
        public decimal OverValue { get; set; }
        public decimal UsedPercent { get; set; }
        public string WarningMessage { get; set; } = string.Empty;
    }
}
