namespace demo1.Validator;

public static class ContractValidator
{
    public static void EnsureValid(decimal contractValue, DateTime? effectiveDate, DateTime? expiredDate)
    {
        if (contractValue < 0)
        {
            throw new ArgumentException("Gia tri hop dong khong duoc am.");
        }

        if (effectiveDate.HasValue && expiredDate.HasValue && expiredDate.Value.Date < effectiveDate.Value.Date)
        {
            throw new ArgumentException("Ngay het han khong duoc nho hon ngay hieu luc.");
        }
    }
}
