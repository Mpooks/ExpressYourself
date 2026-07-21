using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.Caching;

public sealed class MemoryIpInformationCacheTests : IDisposable
{
    private const string Address = "1.2.3.4";

    private static readonly IpInformationCacheEntry CacheEntry = new("Greece", "GR", "GRC");

    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    [Fact]
    public async Task GetAsync_MissingEntry_ReturnsNull()
    {
        var cache = CreateCache();

        var result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsEntry()
    {
        var cache = CreateCache();

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);
        var result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Equal(CacheEntry, result);
    }

    [Fact]
    public async Task GetAsync_EquivalentAddress_ReturnsSameEntry()
    {
        var cache = CreateCache();

        await cache.SetAsync("  1.2.3.4  ", CacheEntry, CancellationToken.None);
        var result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Equal(CacheEntry, result);
    }

    [Fact]
    public async Task RemoveAsync_ExistingEntry_RemovesEntry()
    {
        var cache = CreateCache();

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);
        await cache.RemoveAsync(Address, CancellationToken.None);

        var result = await cache.GetAsync(Address, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_UsesConfiguredTtl()
    {
        var memoryCache = new Mock<IMemoryCache>();
        var cacheEntry = new Mock<ICacheEntry>();

        cacheEntry.SetupAllProperties();

        memoryCache
            .Setup(cache => cache.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry.Object);

        var cache = new MemoryIpInformationCache(
            memoryCache.Object,
            Options.Create(new CacheOptions
            {
                DefaultTtlMinutes = 15
            }));

        await cache.SetAsync(Address, CacheEntry, CancellationToken.None);

        Assert.Equal(TimeSpan.FromMinutes(15), cacheEntry.Object.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task SetAsync_NullEntry_ThrowsArgumentNullException()
    {
        var cache = CreateCache();

        await Assert.ThrowsAsync<ArgumentNullException>(() => cache.SetAsync(Address, null!, CancellationToken.None));
    }

    [Fact]
    public async Task Operations_CancelledToken_ThrowOperationCanceledException()
    {
        var cache = CreateCache();
        using var cancellationTokenSource = new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.GetAsync(Address, cancellationTokenSource.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.SetAsync(Address, CacheEntry, cancellationTokenSource.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => cache.RemoveAsync(Address, cancellationTokenSource.Token));
    }

    private MemoryIpInformationCache CreateCache(int ttlMinutes = 60)
    {
        return new MemoryIpInformationCache(
            _memoryCache,
            Options.Create(new CacheOptions
            {
                DefaultTtlMinutes = ttlMinutes
            }));
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}