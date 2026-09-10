using System.ComponentModel.DataAnnotations;

namespace demo1.Validator;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public class OptionalEmailAddressAttribute : ValidationAttribute
{
    public OptionalEmailAddressAttribute()
    {
        ErrorMessage = "Email không đúng định dạng.";
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (text.Contains('\r') || text.Contains('\n'))
            {
                return false;
            }

            int num = text.IndexOf('@');
            if (num > 0 && num != text.Length - 1)
            {
                return num == text.LastIndexOf('@');
            }
        }

        return false;
    }
}
