namespace ExpressYourself.Application.Caching
{
    public sealed record IpInformationCacheEntry(string? CountryName, string? TwoLetterCode, string? ThreeLetterCode, bool IsUnknown = false)
    {
        public static IpInformationCacheEntry Unknown { get; } = new(CountryName: null, TwoLetterCode: null, ThreeLetterCode: null, IsUnknown: true);
    }
}