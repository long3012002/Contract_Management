namespace demo1.Validator;

public static class RenewalValidator
{
    public static bool IsExpired(DateTime? expiredDate, DateTime today)
    {
        return expiredDate.HasValue && expiredDate.Value.Date < today.Date;
    }

    public static bool IsExpiringSoon(DateTime? expiredDate, DateTime today, int warningDays)
    {
        if (!expiredDate.HasValue)
        {
            return false;
        }

        var daysRemaining = (expiredDate.Value.Date - today.Date).Days;
        return daysRemaining >= 0 && daysRemaining <= warningDays;
    }
}
