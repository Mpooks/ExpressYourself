using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure.Caching.Configuration;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation.Providers;

public sealed class CachedIpInformationProviderTests
{
    private const string Address = "1.2.3.4";
    private static readonly IpInformationDto Dto = new(Address, "GR", "GRC", "Greece");

    private readonly Mock<IIpInformationProvider> _inner = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private CachedIpInformationProvider CreateSut() =>
        new(_inner.Object, _cache, TimeSpan.FromMinutes(60));

    [Fact]
    public async Task Handle_Miss_CallsInner_AndCachesResult()
    {
        
        _inner.Setup(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>())).ReturnsAsync(Dto);
        var sut = CreateSut();

        
        IpInformationDto first = await sut.GetIpInformationAsync(Address, CancellationToken.None);
        IpInformationDto second = await sut.GetIpInformationAsync(Address, CancellationToken.None);

        
        Assert.Equal(Dto, first);
        Assert.Equal(Dto, second);
        _inner.Verify(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentMisses_SameAddress_CallInnerExactlyOnce()
    {
        int calls = 0;
        _inner.Setup(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
              .Returns(async () =>
              {
                  Interlocked.Increment(ref calls);
                  await Task.Delay(50);
                  return Dto;
              });
        var sut = CreateSut();

        IpInformationDto[] results = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => sut.GetIpInformationAsync(Address, CancellationToken.None)));

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Equal(Dto, r));
    }

    [Fact]
    public async Task ConcurrentMisses_DifferentAddresses_DoNotBlockEachOther()
    {
        var dtoA = new IpInformationDto("1.1.1.1", "AU", "AUS", "Australia");
        var dtoB = new IpInformationDto("2.2.2.2", "GR", "GRC", "Greece");
        _inner.Setup(p => p.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>())).ReturnsAsync(dtoA);
        _inner.Setup(p => p.GetIpInformationAsync("2.2.2.2", It.IsAny<CancellationToken>())).ReturnsAsync(dtoB);
        var sut = CreateSut();

        await Task.WhenAll(
            sut.GetIpInformationAsync("1.1.1.1", CancellationToken.None),
            sut.GetIpInformationAsync("2.2.2.2", CancellationToken.None));

        _inner.Verify(p => p.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()), Times.Once);
        _inner.Verify(p => p.GetIpInformationAsync("2.2.2.2", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VariantAddressForms_ShareOneCacheEntry()
    {
        _inner.Setup(p => p.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(Dto);
        var sut = CreateSut();

        await sut.GetIpInformationAsync(Address, CancellationToken.None);
        await sut.GetIpInformationAsync("  1.2.3.4  ", CancellationToken.None);

        _inner.Verify(p => p.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InnerThrowsUnknown_IsNotCached()
    {
        _inner.Setup(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()))
              .ThrowsAsync(new UnknownIpAddressException(Address));
        var sut = CreateSut();

        await Assert.ThrowsAsync<UnknownIpAddressException>(
            () => sut.GetIpInformationAsync(Address, CancellationToken.None));
        await Assert.ThrowsAsync<UnknownIpAddressException>(
            () => sut.GetIpInformationAsync(Address, CancellationToken.None));
        _inner.Verify(p => p.GetIpInformationAsync(Address, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}