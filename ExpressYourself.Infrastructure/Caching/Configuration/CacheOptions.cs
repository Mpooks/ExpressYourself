using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace ExpressYourself.Infrastructure.Caching.Configuration
{
    public sealed class CacheOptions
    {
        public const string SectionName = "Cache";
        public const string MemoryProvider = "Memory";
        public const string RedisProvider = "Redis";

        [Required]
        [RegularExpression(
            "^(?i:Memory|Redis)$",
            ErrorMessage = "Cache provider must be either Memory or Redis.")]
        public string Provider { get; init; } = MemoryProvider;

        [Range(1, 10_080)]
        public int DefaultTtlMinutes { get; init; } = 60;

        public bool UsesMemory => string.Equals(Provider, MemoryProvider, StringComparison.OrdinalIgnoreCase);

        public bool UsesRedis => string.Equals(Provider, RedisProvider, StringComparison.OrdinalIgnoreCase);
    }
}