namespace demo1.DTOs;

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public IDictionary<string, string[]>? Errors { get; set; }
}
