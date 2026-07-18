using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ExpressYourself.Infrastructure.Caching.Configuration
{
    public sealed class CacheOptions
    {
        public const string SectionName = "Cache";

        [Required]
        [RegularExpression(
            "^(Memory|Redis)$",
            ErrorMessage = "Cache provider must be either Memory or Redis.")]
        public string Provider { get; init; } = "Memory";

        [Range(1, 10_080)]
        public int DefaultTtlMinutes { get; init; } = 60;
    }
}