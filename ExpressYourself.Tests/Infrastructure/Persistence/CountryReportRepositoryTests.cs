using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class CountryReportRepositoryTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllAsync_GroupsCountsAndFilters_AgainstRealSqlServer()
    {
        await using var sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build(); await sql.StartAsync();

        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
            .UseSqlServer(sql.GetConnectionString())
            .Options;

        await using (var context = new ExpressYourselfDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Countries.AddRange(new Country("gr", "grc", "Greece"), new Country("us", "usa", "United States"));
            await context.SaveChangesAsync();
            var gr1 = new IpAddress("1.1.1.1"); gr1.SetCountry("GR", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var gr2 = new IpAddress("2.2.2.2"); gr2.SetCountry("GR", new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
            var us1 = new IpAddress("3.3.3.3"); us1.SetCountry("US", new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));
            context.IpAddresses.AddRange(gr1, gr2, us1);
            await context.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlServer"] = sql.GetConnectionString()
            })
            .Build();

        var repository = new CountryReportRepository(new SqlConnectionFactory(configuration));

        var all = await repository.GetAllAsync(null, CancellationToken.None);
        Assert.NotNull(all);
        Assert.Equal(2, all!.Count);
        Assert.Equal("Greece", all[0].CountryName);
        Assert.Equal(2, all[0].AddressesCount);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), all[0].LastAddressUpdated);
        Assert.Equal("United States", all[1].CountryName);
        Assert.Equal(1, all[1].AddressesCount);

        var filtered = await repository.GetAllAsync(new[] { "US" }, CancellationToken.None);
        Assert.Equal("United States", Assert.Single(filtered!).CountryName);

        var none = await repository.GetAllAsync(new[] { "ZZ" }, CancellationToken.None);
        Assert.Empty(none!);
    }
}