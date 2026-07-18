using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ExpressYourself.Infrastructure.Ip2c;

public sealed class Ip2cOptions
{
    public const string SectionName = "Ip2c";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 30)]
    public int TimeoutSeconds { get; init; } = 5;

    [Range(0, 5)]
    public int RetryCount { get; init; } = 3;

    [Range(1, 100)]
    public int CircuitBreakerFailureCount { get; init; } = 5;

    [Range(1, 300)]
    public int CircuitBreakerDurationSeconds { get; init; } = 30;
}