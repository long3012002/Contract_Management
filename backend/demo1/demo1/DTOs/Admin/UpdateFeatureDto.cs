namespace demo1.DTOs
{
    public class UpdateFeatureDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentCode { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; }
    }
}
