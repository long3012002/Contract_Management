namespace demo1.Validator;

public static class PartnerValidator
{
    public static void EnsureValidEmail(string? email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
        {
            throw new ArgumentException("Email đối tác không hợp lệ.");
        }
    }
}
