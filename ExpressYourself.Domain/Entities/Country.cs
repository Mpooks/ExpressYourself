using ExpressYourself.Domain.Exceptions;
using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Domain.Entities;

public sealed class Country
{
    public string TwoLetterCode { get; private set; }

    public string ThreeLetterCode { get; private set; }

    public string CountryName { get; private set; }

    private Country()
    {
        TwoLetterCode = string.Empty;
        ThreeLetterCode = string.Empty;
        CountryName = string.Empty;
    }

    public Country(
        string twoLetterCode,
        string threeLetterCode,
        string countryName)
    {
        TwoLetterCode =
            CountryCodeValidator.Normalize(twoLetterCode, 2);

        ThreeLetterCode =
            CountryCodeValidator.Normalize(threeLetterCode, 3);

        CountryName =
            NormalizeCountryName(countryName);
    }

    public bool UpdateInformation(
        string threeLetterCode,
        string countryName)
    {
        string normalizedThreeLetterCode =
            CountryCodeValidator.Normalize(threeLetterCode, 3);

        string normalizedCountryName =
            NormalizeCountryName(countryName);

        bool hasChanged =
            ThreeLetterCode != normalizedThreeLetterCode || CountryName != normalizedCountryName;

        if (!hasChanged)
        {
            return false;
        }

        ThreeLetterCode = normalizedThreeLetterCode;
        CountryName = normalizedCountryName;

        return true;
    }

    private static string NormalizeCountryName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "Country name is required.");
        }

        string normalizedCountryName = value.Trim();

        if (normalizedCountryName.Length > 100)
        {
            throw new DomainValidationException(
                "Country name cannot exceed 100 characters.");
        }

        return normalizedCountryName;
    }
}