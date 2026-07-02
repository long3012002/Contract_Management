namespace demo1.Validator;

public static class ResolutionValidator
{
    public static void EnsureValidDates(DateTime? issuedDate, DateTime? effectiveDate)
    {
        if (issuedDate.HasValue && effectiveDate.HasValue && effectiveDate.Value.Date < issuedDate.Value.Date)
        {
            throw new ArgumentException("Ngay hieu luc khong duoc nho hon ngay ban hanh.");
        }
    }
}
