using ExpressYourself.Domain.Enums;
using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Domain.Entities;

public sealed class IpAddress
{
    public string Address { get; private set; }

    public string? CountryTwoLetterCode { get; private set; }

    public IpStatus Status { get; private set; }

    public DateTimeOffset? LastCheckedAtUtc { get; private set; }

    public DateTimeOffset? LastUpdatedAtUtc { get; private set; }

    private IpAddress()
    {
        Address = string.Empty;
    }

    public IpAddress(string address)
    {
        Address = IpAddressValidator.Normalize(address);

        CountryTwoLetterCode = null;
        Status = IpStatus.Pending;
        LastCheckedAtUtc = null;
        LastUpdatedAtUtc = null;
    }

    public bool SetCountry(string countryTwoLetterCode, DateTimeOffset checkedAt)
    {
        string normalizedCountryCode = CountryCodeValidator.Normalize(countryTwoLetterCode, 2);

        DateTimeOffset checkedAtUtc = checkedAt.ToUniversalTime();

        LastCheckedAtUtc = checkedAtUtc;

        bool hasChanged = (Status != IpStatus.Success) || (CountryTwoLetterCode != normalizedCountryCode);

        if (!hasChanged)
        {
            return false;
        }

        CountryTwoLetterCode = normalizedCountryCode;
        Status = IpStatus.Success;
        LastUpdatedAtUtc = checkedAtUtc;
        return true;
    }

    public bool MarkAsUnknown(DateTimeOffset checkedAt)
    {
        DateTimeOffset checkedAtUtc = checkedAt.ToUniversalTime();

        LastCheckedAtUtc = checkedAtUtc;

        bool hasChanged = (Status != IpStatus.UnknownIp) || (CountryTwoLetterCode is not null);

        if (!hasChanged)
        {
            return false;
        }

        CountryTwoLetterCode = null;
        Status = IpStatus.UnknownIp;
        LastUpdatedAtUtc = checkedAtUtc;
        return true;
    }
}