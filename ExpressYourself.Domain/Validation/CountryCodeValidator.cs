using ExpressYourself.Domain.Exceptions;

namespace ExpressYourself.Domain.Validation;

internal static class CountryCodeValidator
{
    public static string Normalize(
        string value,
        int requiredLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "Country code is required.");
        }

        string normalizedCode =
            value.Trim().ToUpperInvariant();

        if (normalizedCode.Length != requiredLength)
        {
            throw new DomainValidationException(
                $"Country code must contain exactly {requiredLength} characters.");
        }

        foreach (char character in normalizedCode)
        {
            if (character < 'A' || character > 'Z')
            {
                throw new DomainValidationException(
                    "Country code may contain only English letters.");
            }
        }

        return normalizedCode;
    }
}