using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
    
namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class UnitOfWorkTests
{
    private static ExpressYourselfDbContext GetInMemoryDbContext() =>
    new(new DbContextOptionsBuilder<ExpressYourselfDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options);

    [Fact]
    public async Task SaveChangesAsync_PersistsPendingChanges_AndReturnsAffectedRowCount()
    {
        using var context = GetInMemoryDbContext();
        var unitOfWork = new UnitOfWork(context);
        var repository = new CountryRepository(context);
        repository.Add(new Country("gr", "grc", "Greece"));

        var affected = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(1, affected);
        Assert.NotNull(await repository.GetByTwoLetterCodeAsync("GR", CancellationToken.None));
    }

    [Fact]
    public async Task SaveChangesAsync_WhenNothingChanged_ReturnsZero()
    {
        using var context = GetInMemoryDbContext();
        var unitOfWork = new UnitOfWork(context);

        var affected = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Equal(0, affected);
    }
}