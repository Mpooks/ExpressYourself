using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Enums;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class IpAddressPersistenceTests
{
    [Fact]
    public async Task IpAddress_StateChanges_ArePersistedAndReloaded()
    {
        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var updatedAt = DateTimeOffset.UtcNow;

        using (var context = new ExpressYourselfDbContext(options))
        {
            var repo = new IpAddressRepository(context);
            var ip = new IpAddress("1.2.3.4");
            ip.SetCountry("gr", updatedAt);
            repo.Add(ip);
            await context.SaveChangesAsync();
        }

        using (var context = new ExpressYourselfDbContext(options))
        {
            var reloaded = await new IpAddressRepository(context)
            .GetByAddressAsync("1.2.3.4", CancellationToken.None);

            Assert.NotNull(reloaded);
            Assert.Equal(IpStatus.Success, reloaded!.Status);
            Assert.Equal("GR", reloaded.CountryTwoLetterCode);
            Assert.NotNull(reloaded.LastCheckedAtUtc);
        }
    }
}