namespace ExpressYourself.Application.Interfaces
{
    public sealed record Ip2cLookupResult(Ip2cLookupStatus Status, string? TwoLetterCode, string? ThreeLetterCode, string? CountryName);
}
