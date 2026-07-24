using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Configuration;
using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Enums;
using Microsoft.Extensions.Logging;
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
                    Mock<IIpInformationCache> cache,
        Mock<ILogger<RefreshStoredIpsCommandHandler>> logger) Build(params IpAddress[] stored)
    {
        var client = new Mock<IIp2cClient>();
        var ipRepo = new Mock<IIpAddressRepository>();
        var countryRepo = new Mock<ICountryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<IIpInformationCache>();
        var logger = new Mock<ILogger<RefreshStoredIpsCommandHandler>>();

        ipRepo.SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(stored.ToList())
              .ReturnsAsync(new List<IpAddress>());

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshStoredIpsCommandHandler(
            client.Object,
            ipRepo.Object,
            countryRepo.Object,
            uow.Object,
            cache.Object,
            Options.Create(new RefreshJobOptions()),
            TimeProvider.System,
            logger.Object);

        return (handler, client, ipRepo, countryRepo, uow, cache,logger);
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
        var (handler, client, ipRepo, countryRepo, _, cache,logger) = Build(stored);

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
        var (handler, client, ipRepo, countryRepo, _, cache,logger) = Build(stored);

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
        var (handler, client, _, countryRepo, _, cache,logger) = Build(stored);

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
        var (handler, client, _, _, _, cache,logger) = Build(stored);

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
        var (handler, client, _, countryRepo, _, _,logger) = Build(changingIp, stableIp, failingIp);

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

        logger.Verify(
        log => log.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((state, _) =>
            state.ToString()!.Contains("IP refresh completed") &&
            state.ToString()!.Contains("Scanned: 3") &&
            state.ToString()!.Contains("Changed: 1") &&
            state.ToString()!.Contains("Unchanged: 1") &&
            state.ToString()!.Contains("Failed: 1")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }

    [Fact]
    public async Task Handle_CacheInvalidationThrows_StillCountsChangedNotFailed()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, client, _, countryRepo, _, cache,logger) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ThrowsAsync(new InvalidOperationException("cache down"));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task Handle_PersistFailsForOneIp_IsolatesFailureAndResetsTracker()
    {
        var failing = StoredIp("1.1.1.1", "US");
        var succeeding = StoredIp("2.2.2.2", "US");
        var (handler, client, _, countryRepo, uow, _,logger) = Build(failing, succeeding);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        client.Setup(c => c.GetIpInformationAsync("2.2.2.2", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("GR", "GRC", "Greece"));

        uow.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .ThrowsAsync(new InvalidOperationException("save failed"))
           .ReturnsAsync(1);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Scanned);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Changed);

        uow.Verify(u => u.ClearTracked(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_UnknownIp_MarksUnknownCountsChangedAndInvalidatesThatIpOnly()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, client, ipRepo, _, _, cache,logger) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Failed);
        Assert.Null(stored.CountryTwoLetterCode);
        Assert.Equal(IpStatus.UnknownIp, stored.Status);
        cache.Verify(c => c.RemoveAsync("1.1.1.1", It.IsAny<CancellationToken>()), Times.Once);
        ipRepo.Verify(r => r.GetAddressesByCountryCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownIp_AlreadyUnknown_CountsUnchangedAndDoesNotInvalidate()
    {
        var stored = new IpAddress("1.1.1.1");
        stored.MarkAsUnknown(DateTimeOffset.UtcNow.AddDays(-1));
        var (handler, client, _, _, _, cache,logger) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Unchanged);
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidStatus_CountsFailedAndDoesNotPersistOrInvalidate()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, client, ipRepo, _, uow, cache,logger) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Invalid, null, null, null));

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Changed);
        Assert.Equal(0, result.Unchanged);
        ipRepo.Verify(r => r.Update(It.IsAny<IpAddress>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleFullBatches_PagesUsingLastAddressAsCursor()
    {
        var first = StoredIp("1.1.1.1", "US");
        var second = StoredIp("2.2.2.2", "US");

        var client = new Mock<IIp2cClient>();
        var ipRepo = new Mock<IIpAddressRepository>();
        var countryRepo = new Mock<ICountryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<IIpInformationCache>();

        ipRepo.SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), 1, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<IpAddress> { first })
              .ReturnsAsync(new List<IpAddress> { second })
              .ReturnsAsync(new List<IpAddress>());

        client.Setup(c => c.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "US", "USA", "United States"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("US", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("US", "USA", "United States"));
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshStoredIpsCommandHandler(
            client.Object, ipRepo.Object, countryRepo.Object, uow.Object, cache.Object,
            Options.Create(new RefreshJobOptions { BatchSize = 1 }),
            TimeProvider.System, NullLogger<RefreshStoredIpsCommandHandler>.Instance);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Scanned);
        ipRepo.Verify(r => r.GetBatchAsync(null, 1, It.IsAny<CancellationToken>()), Times.Once);
        ipRepo.Verify(r => r.GetBatchAsync("1.1.1.1", 1, It.IsAny<CancellationToken>()), Times.Once);
        ipRepo.Verify(r => r.GetBatchAsync("2.2.2.2", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class SequenceTimeProvider : TimeProvider
    {
        private readonly Queue<DateTimeOffset> _times;
        public SequenceTimeProvider(params DateTimeOffset[] times) => _times = new Queue<DateTimeOffset>(times);
        public override DateTimeOffset GetUtcNow() => _times.Dequeue();
    }

    [Fact]
    public async Task Handle_StampsEachIpWithClockSampledPerIp_AndPersistsUnchangedChecks()
    {
        var t0 = new DateTimeOffset(2026, 7, 24, 10, 0, 0, TimeSpan.Zero);
        var t1 = t0.AddMinutes(5);

        var first = StoredIp("1.1.1.1", "US");
        var second = StoredIp("2.2.2.2", "US");

        var client = new Mock<IIp2cClient>();
        var ipRepo = new Mock<IIpAddressRepository>();
        var countryRepo = new Mock<ICountryRepository>();
        var uow = new Mock<IUnitOfWork>();
        var cache = new Mock<IIpInformationCache>();

        ipRepo.SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<IpAddress> { first, second })
              .ReturnsAsync(new List<IpAddress>());
        client.Setup(c => c.GetIpInformationAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "US", "USA", "United States"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("US", It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new Country("US", "USA", "United States"));
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshStoredIpsCommandHandler(
            client.Object, ipRepo.Object, countryRepo.Object, uow.Object, cache.Object,
            Options.Create(new RefreshJobOptions()),
            new SequenceTimeProvider(t0, t1),
            NullLogger<RefreshStoredIpsCommandHandler>.Instance);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Unchanged);
        Assert.Equal(t0, first.LastCheckedAtUtc);
        Assert.Equal(t1, second.LastCheckedAtUtc);
        ipRepo.Verify(r => r.Update(It.IsAny<IpAddress>()), Times.Exactly(2));
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyCanceled_ThrowsOperationCanceled()
    {
        var stored = StoredIp("1.1.1.1", "US");
        var (handler, _, _, _, _, _,logger) = Build(stored);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.Handle(new RefreshStoredIpsCommand(), cts.Token));
    }

    [Fact]
    public async Task Handle_SuccessForCountryNotInDatabase_AddsNewCountry()
    {
        var stored = new IpAddress("1.1.1.1");
        var (handler, client, _, countryRepo, _, _,logger) = Build(stored);

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
        countryRepo.Setup(r => r.GetByTwoLetterCodeAsync("GR", It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Country?)null);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Changed);
        countryRepo.Verify(r => r.Add(It.Is<Country>(c =>
                c.TwoLetterCode == "GR" &&
                c.ThreeLetterCode == "GRC" &&
                c.CountryName == "Greece")),
            Times.Once);
    }
}
