using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExpressYourself.Tests.Application.Features.IpRefresh;

public sealed class RefreshStoredIpsCommandHandlerTests
{
    private static (RefreshStoredIpsCommandHandler handler,
                    Mock<IIp2cClient> client,
                    Mock<IIpAddressRepository> ipRepo,
                    Mock<ICountryRepository> countryRepo,
                    Mock<IUnitOfWork> uow) Build(IpAddress stored)
    {
        var client = new Mock<IIp2cClient>();
        var ipRepo = new Mock<IIpAddressRepository>();
        var countryRepo = new Mock<ICountryRepository>();
        var uow = new Mock<IUnitOfWork>();

        ipRepo.SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<IpAddress> { stored })
              .ReturnsAsync(new List<IpAddress>());

        ipRepo.Setup(r => r.GetByAddressAsync(stored.Address, It.IsAny<CancellationToken>()))
              .ReturnsAsync(stored);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ReturnsAsync(1);

        var handler = new RefreshStoredIpsCommandHandler(
            client.Object,
            ipRepo.Object,
            countryRepo.Object,
            uow.Object,
            TimeProvider.System,
            NullLogger<RefreshStoredIpsCommandHandler>.Instance);

        return (handler, client, ipRepo, countryRepo, uow);
    }

    [Fact]
    public async Task Handle_CountryChanged_CountsChanged()
    {
        var stored = new IpAddress("1.1.1.1");
        stored.SetCountry("US", DateTimeOffset.UtcNow.AddDays(-1));   // currently US
        var (handler, client, _, countryRepo, _) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece")); // now GR
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Unchanged);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Handle_SameCountry_CountsUnchanged()
    {
        var stored = new IpAddress("1.1.1.1");
        stored.SetCountry("US", DateTimeOffset.UtcNow.AddDays(-1));
        var (handler, client, _, countryRepo, _) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "US", "USA", "United States"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("US", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("US", "USA", "United States"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.Changed);
        Assert.Equal(1, result.Unchanged);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Handle_LookupThrows_CountsFailed_AndDoesNotCrash()
    {
        var stored = new IpAddress("1.1.1.1");
        var (handler, client, _, _, _) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ThrowsAsync(new HttpRequestException("ip2c down"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.Changed);
        Assert.Equal(0, result.Unchanged);
        Assert.Equal(1, result.Failed);
    }
}