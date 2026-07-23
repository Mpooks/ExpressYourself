using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Configuration;
using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Application.Features.IpRefresh;

public sealed class RefreshStoredIpsCommandHandlerTests
{
    private static (RefreshStoredIpsCommandHandler handler,
                    Mock<IIp2cClient> client,
                    Mock<IIpAddressRepository> ipRepo,
                    Mock<ICountryRepository> countryRepo,
                    Mock<IUnitOfWork> uow,
                    Mock<IIpInformationCache> cache) Build(params IpAddress[] stored)
    {
        var client = new Mock<IIp2cClient>();
        var ipRepo = new Mock<IIpAddressRepository>();
        var countryRepo = new Mock<ICountryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<IIpInformationCache>();

        ipRepo.SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(stored.ToList())
              .ReturnsAsync(new List<IpAddress>());

        foreach (var ip in stored)
        {
            ipRepo.Setup(r => r.GetByAddressAsync(ip.Address, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(ip);
        }

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshStoredIpsCommandHandler(
            client.Object,
            ipRepo.Object,
            countryRepo.Object,
            uow.Object,
            cache.Object,
            Options.Create(new RefreshJobOptions()),
            TimeProvider.System,
            NullLogger<RefreshStoredIpsCommandHandler>.Instance);

        return (handler, client, ipRepo, countryRepo, uow, cache);
    }

    private static IpAddress StoredIp(string address, string country)
    {
        var ip = new IpAddress(address);
        ip.SetCountry(country, DateTimeOffset.UtcNow.AddDays(-1));
        return ip;
    }

    [Fact]
    public async Task Handle_IpCountryChanged_CountsChangedAndInvalidatesThatIpOnly()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, client, ipRepo, countryRepo, _, cache) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Changed);
        cache.Verify(c => c.RemoveAsync("1.1.1.1", It.IsAny<CancellationToken>()), Times.Once);
        ipRepo.Verify(r => r.GetAddressesByCountryCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CountryMetadataChanged_InvalidatesAllAddressesForThatCountry()
    {
        var stored = StoredIp("1.1.1.1", "GR");
        var (handler, client, ipRepo, countryRepo, _, cache) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Hellas"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));
        ipRepo.Setup(r => r.GetAddressesByCountryCodeAsync("GR", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<string> { "1.1.1.1", "9.9.9.9" });

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Unchanged);
        cache.Verify(c => c.RemoveAsync("1.1.1.1", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("9.9.9.9", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameCountry_CountsUnchangedAndDoesNotInvalidate()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, client, _, countryRepo, _, cache) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "US", "USA", "United States"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("US", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("US", "USA", "United States"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Unchanged);
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LookupThrows_CountsFailed()
    {
        var stored = new IpAddress("1.1.1.1");
        var (handler, client, _, _, _, cache) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("ip2c down"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Failed);
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleIps_TalliesEveryOutcomeIndependently()
    {
        var changingIp = StoredIp("1.1.1.1", "US");
        var stableIp = StoredIp("2.2.2.2", "US");
        var failingIp = new IpAddress("3.3.3.3");
        var (handler, client, _, countryRepo, _, _) = Build(changingIp, stableIp, failingIp);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        client.Setup(c => c.GetIpInformationAsync("2.2.2.2", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "US", "USA", "United States"));
        client.Setup(c => c.GetIpInformationAsync("3.3.3.3", It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("down"));

        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("US", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("US", "USA", "United States"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(3, result.Scanned);
        Assert.Equal(1, result.Changed);
        Assert.Equal(1, result.Unchanged);
        Assert.Equal(1, result.Failed);
    }
}