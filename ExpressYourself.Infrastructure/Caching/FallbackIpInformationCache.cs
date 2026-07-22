using ExpressYourself.Application.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExpressYourself.Infrastructure.Caching;

internal sealed class FallbackIpInformationCache : IIpInformationCache
{
    private readonly IIpInformationCache _redisCache;
    private readonly IIpInformationCache _memoryCache;
    private readonly ILogger<FallbackIpInformationCache> _logger;

    public FallbackIpInformationCache(
        IIpInformationCache redisCache,
        IIpInformationCache memoryCache,
        ILogger<FallbackIpInformationCache> logger)
    {
        ArgumentNullException.ThrowIfNull(redisCache);
        ArgumentNullException.ThrowIfNull(memoryCache);
        ArgumentNullException.ThrowIfNull(logger);

        _redisCache = redisCache;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<IpInformationCacheEntry?> GetAsync(string address, CancellationToken cancellationToken)
    {
        try
        {
            return await _redisCache.GetAsync(address, cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            LogFallback(exception, "read");

            return await _memoryCache.GetAsync(address, cancellationToken);
        }
    }

    public async Task SetAsync(string address, IpInformationCacheEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await _redisCache.SetAsync(address, entry, cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            LogFallback(exception, "write");

            await _memoryCache.SetAsync(address, entry, cancellationToken);

            return;
        }

        await _memoryCache.SetAsync(address, entry, cancellationToken);
    }

    public async Task RemoveAsync(string address, CancellationToken cancellationToken)
    {
        try
        {
            await _redisCache.RemoveAsync(address, cancellationToken);
        }
        catch (Exception exception) when (IsRedisUnavailable(exception))
        {
            LogFallback(exception, "remove");
        }

        await _memoryCache.RemoveAsync(address, cancellationToken);
    }

    private static bool IsRedisUnavailable(Exception exception)
    {
        return exception is RedisException or RedisTimeoutException;
    }

    private void LogFallback(Exception exception, string operation)
    {
        _logger.LogWarning(exception, "Redis cache {Operation} failed. Using memory cache fallback.", operation);
    }
}
