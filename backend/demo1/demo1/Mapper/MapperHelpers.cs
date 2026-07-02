namespace demo1.Mapper;

public static class MapperHelpers
{
    public static string NormalizeCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    public static string TrimRequired(string value)
    {
        return value.Trim();
    }

    public static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
