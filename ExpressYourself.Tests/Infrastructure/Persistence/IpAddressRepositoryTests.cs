using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class IpAddressRepositoryTests
{
    private static ExpressYourselfDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ExpressYourselfDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByAddressAsync_ValidData_ShouldReturnIpAddress()
    {

        using var context = GetInMemoryDbContext();
        var repository = new IpAddressRepository(context);
        var ip = new IpAddress("1.2.3.4");

        repository.Add(ip);
        await context.SaveChangesAsync();

        var result = await repository.GetByAddressAsync("1.2.3.4", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("1.2.3.4", result.Address);
    }

    [Fact]
    public async Task GetByAddressAsync_WhenNotExists_ShouldReturnNull()
    {

        using var context = GetInMemoryDbContext();
        var repository = new IpAddressRepository(context);

        var result = await repository.GetByAddressAsync("9.9.9.9", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAddressesByCountryCodeAsync_WhenMatchesExist_ShouldReturnOnlyMatchingAddresses()
    {
        using var context = GetInMemoryDbContext();
        var repository = new IpAddressRepository(context);

        context.Countries.AddRange(new Country("GR", "GRC", "Greece"), new Country("US", "USA", "United States"));

        var firstGreekIp = new IpAddress("1.1.1.1");
        firstGreekIp.SetCountry("GR", DateTimeOffset.UtcNow);

        var secondGreekIp = new IpAddress("2.2.2.2");
        secondGreekIp.SetCountry("GR", DateTimeOffset.UtcNow);

        var americanIp = new IpAddress("3.3.3.3");
        americanIp.SetCountry("US", DateTimeOffset.UtcNow);

        repository.Add(firstGreekIp);
        repository.Add(secondGreekIp);
        repository.Add(americanIp);

        await context.SaveChangesAsync();

        IReadOnlyList<string> result = await repository.GetAddressesByCountryCodeAsync("GR", CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains("1.1.1.1", result);
        Assert.Contains("2.2.2.2", result);
        Assert.DoesNotContain("3.3.3.3", result);
    }

    [Fact]
    public async Task GetAddressesByCountryCodeAsync_WhenNoMatches_ShouldReturnEmptyList()
    {
        using var context = GetInMemoryDbContext();
        var repository = new IpAddressRepository(context);

        IReadOnlyList<string> result =
            await repository.GetAddressesByCountryCodeAsync(
                "GR",
                CancellationToken.None);

        Assert.Empty(result);
    }
}