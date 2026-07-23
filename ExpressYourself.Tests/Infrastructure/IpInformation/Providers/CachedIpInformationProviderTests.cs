using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation.Providers;

public sealed class CachedIpInformationProviderTests
{
    private const string Address = "1.2.3.4";

    private static readonly IpInformationDto Dto = new(Address, "GR", "GRC", "Greece");

    private static readonly IpInformationCacheEntry CacheEntry = new("Greece", "GR", "GRC");

    private readonly Mock<IIpInformationProvider> _inner = new();
    private readonly Mock<IIpInformationCache> _cache = new();

    [Fact]
    public async Task GetIpInformationAsync_CacheHit_ReturnsCachedEntry()
    {
        _cache
            .Setup(cache => cache.GetAsync(Address, CancellationToken.None))
            .ReturnsAsync(CacheEntry);

        var sut = CreateSut();

        IpInformationDto result =
            await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal(Dto, result);

        _inner.Verify(
            provider => provider.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _cache.Verify(
            cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<IpInformationCacheEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetIpInformationAsync_CacheMiss_CallsInnerAndCachesResult()
    {
        SetupEmptyCache();

        _inner
            .Setup(provider => provider.GetIpInformationAsync(Address, CancellationToken.None))
            .ReturnsAsync(Dto);

        var sut = CreateSut();

        IpInformationDto result =
            await sut.GetIpInformationAsync(Address, CancellationToken.None);

        Assert.Equal(Dto, result);

        _inner.Verify(
            provider => provider.GetIpInformationAsync(Address, CancellationToken.None), Times.Once);

        _cache.Verify(
            cache => cache.SetAsync(
                Address,
                It.Is<IpInformationCacheEntry>(entry =>
                    entry.CountryName == Dto.CountryName &&
                    entry.TwoLetterCode == Dto.TwoLetterCountryCode &&
                    entry.ThreeLetterCode == Dto.ThreeLetterCountryCode),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentMisses_SameAddress_CallInnerOnce()
    {
        SetupStatefulCache();

        int calls = 0;

        _inner
            .Setup(provider => provider.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken cancellationToken) =>
            {
                Interlocked.Increment(ref calls);
                await Task.Delay(50, cancellationToken);

                return Dto;
            });

        var sut = CreateSut();

        IpInformationDto[] results = await Task.WhenAll(
            Enumerable.Range(0, 20)
                .Select(_ => sut.GetIpInformationAsync(Address, CancellationToken.None)));

        Assert.Equal(1, calls);
        Assert.All(results, result => Assert.Equal(Dto, result));
    }

    [Fact]
    public async Task ConcurrentMisses_DifferentAddresses_DoNotBlockEachOther()
    {
        const string firstAddress = "1.1.1.1";
        const string secondAddress = "2.2.2.2";

        var firstDto =
            new IpInformationDto(firstAddress, "AU", "AUS", "Australia");

        var secondDto =
            new IpInformationDto(secondAddress, "GR", "GRC", "Greece");

        SetupEmptyCache();

        int startedCalls = 0;

        var bothCallsStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _inner
            .Setup(provider => provider.GetIpInformationAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (
                string address,
                CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref startedCalls) == 2)
                {
                    bothCallsStarted.TrySetResult(true);
                }

                await bothCallsStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);

                return address == firstAddress
                    ? firstDto
                    : secondDto;
            });

        var sut = CreateSut();

        IpInformationDto[] results = await Task.WhenAll(
            sut.GetIpInformationAsync(
                firstAddress,
                CancellationToken.None),
            sut.GetIpInformationAsync(
                secondAddress,
                CancellationToken.None));

        Assert.Contains(firstDto, results);
        Assert.Contains(secondDto, results);
        Assert.Equal(2, startedCalls);
    }

    [Fact]
    public async Task EquivalentAddressForms_ShareOneCacheEntry()
    {
        SetupStatefulCache();

        _inner
            .Setup(provider => provider.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dto);

        var sut = CreateSut();

        await sut.GetIpInformationAsync("  1.2.3.4  ", CancellationToken.None);

        await sut.GetIpInformationAsync(Address, CancellationToken.None);

        _inner.Verify(
            provider => provider.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InnerThrowsUnknown_CachesResultAndSkipsNextInnerCall()
    {
        SetupStatefulCache();

        _inner
            .Setup(provider => provider.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnknownIpAddressException(Address));

        var sut = CreateSut();

        await Assert.ThrowsAsync<UnknownIpAddressException>(() => sut.GetIpInformationAsync(Address, CancellationToken.None));

        await Assert.ThrowsAsync<UnknownIpAddressException>(() => sut.GetIpInformationAsync(Address, CancellationToken.None));

        _inner.Verify(provider => provider.GetIpInformationAsync(Address,It.IsAny<CancellationToken>()), Times.Once);

        _cache.Verify(cache => cache.SetAsync(Address, It.Is<IpInformationCacheEntry>(entry => entry.IsUnknown), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InnerThrowsUnexpectedError_DoesNotCacheResult()
    {
        SetupEmptyCache();

        _inner
            .Setup(provider => provider.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider failed."));

        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetIpInformationAsync(Address, CancellationToken.None));

        _cache.Verify(cache => cache.SetAsync(It.IsAny<string>(), It.IsAny<IpInformationCacheEntry>(),It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelledWaiter_DoesNotStartAnotherInnerRequest()
    {
        SetupEmptyCache();

        var innerStarted = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously);

        var releaseInner = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously);

        _inner
            .Setup(provider => provider.GetIpInformationAsync(
                Address,
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken cancellationToken) =>
            {
                innerStarted.TrySetResult(true);
                await releaseInner.Task.WaitAsync(cancellationToken);

                return Dto;
            });

        var sut = CreateSut();

        Task<IpInformationDto> firstRequest =
            sut.GetIpInformationAsync(Address, CancellationToken.None);

        await innerStarted.Task;

        using var cancellationTokenSource = new CancellationTokenSource();

        Task<IpInformationDto> waitingRequest =
            sut.GetIpInformationAsync(Address, cancellationTokenSource.Token);

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => waitingRequest);

        releaseInner.TrySetResult(true);

        Assert.Equal(Dto, await firstRequest);

        _inner.Verify(
            provider => provider.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()), Times.Once);
    }

    private CachedIpInformationProvider CreateSut()
    {
        return new CachedIpInformationProvider(
            _inner.Object,
            _cache.Object);
    }

    private void SetupEmptyCache()
    {
        _cache
            .Setup(cache => cache.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IpInformationCacheEntry?)null);

        _cache
            .Setup(cache => cache.SetAsync(
                It.IsAny<string>(),
                It.IsAny<IpInformationCacheEntry>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupStatefulCache()
    {
        IpInformationCacheEntry? storedEntry = null;

        _cache
            .Setup(cache => cache.GetAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => storedEntry);

        _cache
            .Setup(cache => cache.SetAsync(
                It.IsAny<string>(),
                It.IsAny<IpInformationCacheEntry>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, IpInformationCacheEntry, CancellationToken>((address, entry, cancellationToken) => storedEntry = entry)
            .Returns(Task.CompletedTask);
    }
}