using System.ComponentModel.DataAnnotations;

namespace ExpressYourself.API.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public const string IpLookupPolicy = "ip-lookup";
    public const string CountryReportPolicy = "country-report";

    public RateLimitPolicyOptions IpLookup { get; init; } = new();

    public RateLimitPolicyOptions CountryReport { get; init; } = new();
}

public sealed class RateLimitPolicyOptions
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; init; } = 10;

    [Range(1, 3_600)]
    public int WindowSeconds { get; init; } = 10;
}
