namespace demo1.Validator;

public static class ContractValidator
{
    public static void EnsureValid(decimal contractValue, DateTime? effectiveDate, DateTime? expiredDate)
    {
        if (contractValue < 0)
        {
            throw new ArgumentException("Giá trị hợp đồng không được âm.");
        }

        if (effectiveDate.HasValue && expiredDate.HasValue && expiredDate.Value.Date < effectiveDate.Value.Date)
        {
            throw new ArgumentException("Ngày hết hạn không được nhỏ hơn ngày hiệu lực.");
        }
    }
}
