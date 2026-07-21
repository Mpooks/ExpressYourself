using System.Text;
using System.Text.Json;
using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.Caching;

public sealed class RedisIpInformationCacheTests
{
    private const string Address = "1.2.3.4";
    private const string CacheKey = "ip-info:1.2.3.4";

    private static readonly IpInformationCacheEntry CacheEntry = new("Greece", "GR", "GRC");

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly Mock<IDistributedCache> _distributedCache = new();

    [Fact]
    public async Task GetAsync_MissingEntry_ReturnsNull()
    {
        _distributedCache
            .Setup(cache => cache.GetAsync(CacheKey, CancellationToken.None))
            .ReturnsAsync((byte[]?)null);

        var cache = CreateCache();

        IpInformationCacheEntry? result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Null(result);

        _distributedCache.Verify(
            cache => cache.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ValidEntry_ReturnsDeserializedEntry()
    {
        byte[] value = JsonSerializer.SerializeToUtf8Bytes(CacheEntry,SerializerOptions);

        _distributedCache
            .Setup(cache => cache.GetAsync(CacheKey, CancellationToken.None))
            .ReturnsAsync(value);

        var cache = CreateCache();

        IpInformationCacheEntry? result = await cache.GetAsync( Address, CancellationToken.None);

        Assert.Equal(CacheEntry, result);
    }

    [Fact]
    public async Task GetAsync_InvalidJson_RemovesEntryAndReturnsNull()
    {
        byte[] invalidValue = Encoding.UTF8.GetBytes("not-json");

        _distributedCache
            .Setup(cache => cache.GetAsync(CacheKey, CancellationToken.None))
            .ReturnsAsync(invalidValue);

        _distributedCache
            .Setup(cache => cache.RemoveAsync(CacheKey, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var cache = CreateCache();

        IpInformationCacheEntry? result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Null(result);

        _distributedCache.Verify(cache => cache.RemoveAsync(CacheKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SetAsync_SerializesEntryAndUsesConfiguredTtl()
    {
        byte[]? storedValue = null;
        DistributedCacheEntryOptions? storedOptions = null;

        _distributedCache
            .Setup(cache => cache.SetAsync(CacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), CancellationToken.None))
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (key, value, options, cancellationToken) =>
                {
                    storedValue = value;
                    storedOptions = options;
                })
            .Returns(Task.CompletedTask);

        var cache = CreateCache(ttlMinutes: 15);

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);

        Assert.NotNull(storedValue);
        Assert.NotNull(storedOptions);

        IpInformationCacheEntry? storedEntry = JsonSerializer.Deserialize<IpInformationCacheEntry>(storedValue, SerializerOptions);

        Assert.Equal(CacheEntry, storedEntry);
        Assert.Equal(TimeSpan.FromMinutes(15), storedOptions.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task RemoveAsync_EquivalentAddress_UsesNormalizedKey()
    {
        _distributedCache
            .Setup(cache => cache.RemoveAsync(CacheKey, CancellationToken.None))
            .Returns(Task.CompletedTask);

        var cache = CreateCache();

        await cache.RemoveAsync("  1.2.3.4  ", CancellationToken.None);

        _distributedCache.Verify(cache => cache.RemoveAsync( CacheKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Operations_CancelledToken_ThrowOperationCanceledException()
    {
        var cache = CreateCache();

        using var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            cache.GetAsync(Address, cancellationTokenSource.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            cache.SetAsync(Address, CacheEntry, cancellationTokenSource.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            cache.RemoveAsync(Address, cancellationTokenSource.Token));

        _distributedCache.VerifyNoOtherCalls();
    }

    private RedisIpInformationCache CreateCache(
        int ttlMinutes = 60)
    {
        return new RedisIpInformationCache(_distributedCache.Object,Options.Create(new CacheOptions{DefaultTtlMinutes = ttlMinutes}), NullLogger<RedisIpInformationCache>.Instance);
    }
}
