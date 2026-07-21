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
    public async Task Add_And_GetByAddressAsync_ShouldReturnIpAddress()
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
}