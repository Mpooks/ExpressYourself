using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace ExpressYourself.Tests.Infrastructure.Caching;

public sealed class FallbackIpInformationCacheTests
{
    private const string Address = "1.2.3.4";

    private static readonly IpInformationCacheEntry CacheEntry = new("Greece", "GR", "GRC");

    private readonly Mock<IIpInformationCache> _redisCache = new();
    private readonly Mock<IIpInformationCache> _memoryCache = new();

    [Fact]
    public async Task GetAsync_RedisAvailable_ReturnsRedisEntry()
    {
        _redisCache
            .Setup(cache => cache.GetAsync(Address, CancellationToken.None))
            .ReturnsAsync(CacheEntry);

        var cache = CreateCache();

        IpInformationCacheEntry? result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Equal(CacheEntry, result);
        _memoryCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_RedisConnectionFails_ReturnsMemoryEntry()
    {
        _redisCache
            .Setup(cache => cache.GetAsync(Address, CancellationToken.None))
            .ThrowsAsync(CreateConnectionException());

        _memoryCache
            .Setup(cache => cache.GetAsync(Address, CancellationToken.None))
            .ReturnsAsync(CacheEntry);

        var cache = CreateCache();

        IpInformationCacheEntry? result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Equal(CacheEntry, result);

        _memoryCache.Verify(cache => cache.GetAsync(Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SetAsync_RedisAvailable_WritesBothCaches()
    {
        _redisCache
            .Setup(cache => cache.SetAsync(Address, CacheEntry, CancellationToken.None))
            .Returns(Task.CompletedTask);

        _memoryCache
            .Setup(cache => cache.SetAsync(Address, CacheEntry, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var cache = CreateCache();

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);

        _redisCache.Verify(redis => redis.SetAsync(Address, CacheEntry, CancellationToken.None), Times.Once);

        _memoryCache.Verify(memory => memory.SetAsync(Address, CacheEntry, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SetAsync_RedisConnectionFails_WritesMemoryCache()
    {
        _redisCache
            .Setup(cache => cache.SetAsync(Address, CacheEntry, CancellationToken.None))
            .ThrowsAsync(CreateConnectionException());

        _memoryCache
            .Setup(cache => cache.SetAsync(Address, CacheEntry, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var cache = CreateCache();

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);

        _memoryCache.Verify(
            memory => memory.SetAsync(Address, CacheEntry, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RedisTimeout_RemovesMemoryEntry()
    {
        _redisCache
            .Setup(cache => cache.RemoveAsync(Address, CancellationToken.None))
            .ThrowsAsync(new RedisTimeoutException("Redis operation timed out.", CommandStatus.Unknown));

        _memoryCache
            .Setup(cache => cache.RemoveAsync(Address, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var cache = CreateCache();

        await cache.RemoveAsync(Address, CancellationToken.None);

        _memoryCache.Verify(memory => memory.RemoveAsync( Address, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetAsync_NonRedisFailure_DoesNotUseFallback()
    {
        _redisCache
            .Setup(cache => cache.GetAsync(Address, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Unexpected cache error."));

        var cache = CreateCache();

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetAsync(Address, CancellationToken.None));

        _memoryCache.VerifyNoOtherCalls();
    }

    private FallbackIpInformationCache CreateCache()
    {
        return new FallbackIpInformationCache(_redisCache.Object, _memoryCache.Object, NullLogger<FallbackIpInformationCache>.Instance);
    }

    private static RedisConnectionException CreateConnectionException()
    {
        return new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis is unavailable.");
    }
}
