using ExpressYourself.Domain.Enums;
using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Domain.Entities;

public sealed class IpAddress
{
    public string Address { get; private set; }

    public string? CountryTwoLetterCode { get; private set; }

    public IpStatus Status { get; private set; }

    public DateTimeOffset? LastUpdated { get; private set; }

    private IpAddress()
    {
        Address = string.Empty;
    }

    public IpAddress(string address)
    {
        Address = IpAddressValidator.Normalize(address);

        CountryTwoLetterCode = null;
        Status = IpStatus.Pending;
        LastUpdated = null;
    }

    public bool SetCountry(
        string countryTwoLetterCode,
        DateTimeOffset updatedAt)
    {
        string normalizedCountryCode =
            CountryCodeValidator.Normalize(
                countryTwoLetterCode,
                2);

        DateTimeOffset updatedAtUtc =
            updatedAt.ToUniversalTime();

        bool hasChanged =
            Status != IpStatus.Success || CountryTwoLetterCode != normalizedCountryCode;

        if (!hasChanged)
        {
            return false;
        }

        CountryTwoLetterCode = normalizedCountryCode;
        Status = IpStatus.Success;
        LastUpdated = updatedAtUtc;

        return true;
    }

    public bool MarkAsUnknown(
        DateTimeOffset updatedAt)
    {
        DateTimeOffset updatedAtUtc =
            updatedAt.ToUniversalTime();

        bool hasChanged =
            Status != IpStatus.UnknownIp || CountryTwoLetterCode is not null;

        if (!hasChanged)
        {
            return false;
        }

        CountryTwoLetterCode = null;
        Status = IpStatus.UnknownIp;
        LastUpdated = updatedAtUtc;

        return true;
    }
}