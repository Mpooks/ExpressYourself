using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Configuration;
using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Testcontainers.MsSql;

namespace ExpressYourself.Tests.Application.Features.IpRefresh;

public sealed class RefreshStoredIpsCommandHandlerIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Handle_OneIpFailsToPersist_IsolatesFailureAndStillPersistsTheRest()
    {
        await using var sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await sql.StartAsync();

        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
            .UseSqlServer(sql.GetConnectionString())
            .Options;

        await using (var seed = new ExpressYourselfDbContext(options))
        {
            await seed.Database.EnsureCreatedAsync();
            seed.Countries.AddRange(
                new Country("US", "USA", "United States"),
                new Country("GR", "GRC", "Greece"));

            var first = new IpAddress("1.1.1.1"); first.SetCountry("US", DateTimeOffset.UtcNow);
            var second = new IpAddress("2.2.2.2"); second.SetCountry("US", DateTimeOffset.UtcNow);
            seed.IpAddresses.AddRange(first, second);
            await seed.SaveChangesAsync();
        }

        var client = new Mock<IIp2cClient>();

        client.Setup(c => c.GetIpInformationAsync("1.1.1.1", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "ZZ", "USA", "Bogus"));

        client.Setup(c => c.GetIpInformationAsync("2.2.2.2", It.IsAny<CancellationToken>()))
              .ReturnsAsync(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));

        var cache = new Mock<IIpInformationCache>();
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);

        await using var context = new ExpressYourselfDbContext(options);
        var handler = new RefreshStoredIpsCommandHandler(
            client.Object,
            new IpAddressRepository(context),
            new CountryRepository(context),
            new UnitOfWork(context),
            cache.Object,
            Options.Create(new RefreshJobOptions()),
            TimeProvider.System,
            NullLogger<RefreshStoredIpsCommandHandler>.Instance);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Scanned);
        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Unchanged);

        await using var verify = new ExpressYourselfDbContext(options);

        var reloadedFirst = await verify.IpAddresses.AsNoTracking().SingleAsync(i => i.Address == "1.1.1.1");
        var reloadedSecond = await verify.IpAddresses.AsNoTracking().SingleAsync(i => i.Address == "2.2.2.2");

        Assert.Equal("US", reloadedFirst.CountryTwoLetterCode);
        Assert.Equal("GR", reloadedSecond.CountryTwoLetterCode);
        Assert.False(await verify.Countries.AsNoTracking().AnyAsync(c => c.TwoLetterCode == "ZZ"));
    }
}