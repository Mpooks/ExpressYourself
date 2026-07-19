namespace ExpressYourself.Gateway.Ip2c
{
    public sealed record Ip2cLookupResult(Ip2cLookupStatus Status, string? TwoLetterCode, string? ThreeLetterCode, string? CountryName);
}
