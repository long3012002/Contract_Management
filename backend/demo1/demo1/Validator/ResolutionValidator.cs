namespace demo1.Validator;

public static class ResolutionValidator
{
    public static void EnsureValidDates(DateTime? issuedDate, DateTime? effectiveDate)
    {
        if (issuedDate.HasValue && effectiveDate.HasValue && effectiveDate.Value.Date < issuedDate.Value.Date)
        {
            throw new ArgumentException("Ngày hiệu lực không được nhỏ hơn ngày ban hành.");
        }
    }
}
