using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Redis;

namespace ExpressYourself.Tests.Infrastructure.Caching;

public sealed class RedisIpInformationCacheIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SetGetAndRemoveAsync_UsesRealRedis()
    {
        await using var redisContainer = new RedisBuilder("redis:7.4-alpine")
                .Build();

        await redisContainer.StartAsync();

        string instanceName = $"expressyourself-tests:{Guid.NewGuid():N}:";

        using var distributedCache = new RedisCache(
                Options.Create(new RedisCacheOptions
                    {
                        Configuration =
                            redisContainer.GetConnectionString(),
                        InstanceName = instanceName
                    }));

        var cache = new RedisIpInformationCache(distributedCache,
                Options.Create(
                    new CacheOptions
                    {
                        Provider = "Redis",
                        DefaultTtlMinutes = 5
                    }),
                NullLogger<RedisIpInformationCache>.Instance);

        const string address = "203.0.113.10";

        var entry = new IpInformationCacheEntry("Greece", "GR", "GRC");

        await cache.SetAsync(address, entry, CancellationToken.None);

        IpInformationCacheEntry? cachedEntry = await cache.GetAsync(address, CancellationToken.None);

        Assert.Equal(entry, cachedEntry);

        await cache.RemoveAsync(address, CancellationToken.None);

        IpInformationCacheEntry? removedEntry = await cache.GetAsync(address, CancellationToken.None);

        Assert.Null(removedEntry);
    }
}
