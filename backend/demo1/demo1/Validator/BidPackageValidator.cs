namespace demo1.Validator;

public static class BidPackageValidator
{
    public static void EnsureValid(decimal estimatedValue, decimal warningThresholdPercent)
    {
        if (estimatedValue < 0)
        {
            throw new ArgumentException("Gia tri du toan goi thau khong duoc am.");
        }

        if (warningThresholdPercent <= 0 || warningThresholdPercent > 100)
        {
            throw new ArgumentException("Nguong canh bao phai nam trong khoang 1 den 100.");
        }
    }
}
