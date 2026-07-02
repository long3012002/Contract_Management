namespace demo1.Validator;

public static class ProjectValidator
{
    public static void EnsureValidBudget(decimal totalBudget)
    {
        if (totalBudget < 0)
        {
            throw new ArgumentException("Tong ngan sach du an khong duoc am.");
        }
    }
}
