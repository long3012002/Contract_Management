namespace demo1.Validator;

public static class PartnerValidator
{
    public static void EnsureValidEmail(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            throw new ArgumentException("Email doi tac khong hop le.");
        }
    }
}
