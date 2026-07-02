namespace demo1.Validator;

public static class BudgetValidator
{
    public static bool IsOverBudget(decimal contractValue, decimal estimatedValue, decimal warningThresholdPercent)
    {
        if (estimatedValue <= 0)
        {
            return false;
        }

        var limitValue = estimatedValue * warningThresholdPercent / 100;
        return contractValue > limitValue;
    }

    public static decimal CalculateUsedPercent(decimal contractValue, decimal estimatedValue)
    {
        if (estimatedValue <= 0)
        {
            return 0;
        }

        return Math.Round(contractValue / estimatedValue * 100, 2);
    }
}
