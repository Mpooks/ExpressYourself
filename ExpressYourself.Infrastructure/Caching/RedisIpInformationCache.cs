using System.Text.Json;
using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpressYourself.Infrastructure.Caching;

internal sealed class RedisIpInformationCache : IIpInformationCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisIpInformationCache> _logger;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _negativeTtl;

    public RedisIpInformationCache(IDistributedCache cache, IOptions<CacheOptions> options, ILogger<RedisIpInformationCache> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cache = cache;
        _logger = logger;
        _ttl = TimeSpan.FromMinutes(options.Value.DefaultTtlMinutes);
        _negativeTtl =
            TimeSpan.FromSeconds(options.Value.NegativeTtlSeconds);
    }

    public async Task<IpInformationCacheEntry?> GetAsync(string address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string key = IpInformationCacheKeys.ForAddress(address);

        byte[]? value = await _cache.GetAsync(key, cancellationToken);

        if (value is null)
        {
            return null;
        }

        try
        {
            IpInformationCacheEntry? entry = JsonSerializer.Deserialize<IpInformationCacheEntry>(value, SerializerOptions);

            if (entry is not null)
            {
                return entry;
            }
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid cache entry found for IP address {Address}.", address);
        }

        await _cache.RemoveAsync(key, cancellationToken);

        return null;
    }

    public async Task SetAsync(string address, IpInformationCacheEntry entry, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entry);

        string key = IpInformationCacheKeys.ForAddress(address);

        byte[] value = JsonSerializer.SerializeToUtf8Bytes(entry, SerializerOptions);

        TimeSpan ttl = entry.IsUnknown
            ? _negativeTtl
            : _ttl;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        await _cache.SetAsync(key, value, options, cancellationToken);
    }

    public async Task RemoveAsync(string address, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string key = IpInformationCacheKeys.ForAddress(address);

        await _cache.RemoveAsync(key, cancellationToken);
    }
}