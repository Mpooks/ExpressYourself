using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class CountryRepositoryTests
{
    
    private static ExpressYourselfDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ExpressYourselfDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByTwoLetterCodeAsync_ValidData_ShouldReturnCountry()
    {
      
        using var context = GetInMemoryDbContext();
        var repository = new CountryRepository(context);
        var country = new Country("gr", "grc", "Greece");

        repository.Add(country);
        await context.SaveChangesAsync(); 

        var result = await repository.GetByTwoLetterCodeAsync("GR", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("Greece", result.CountryName);
    }

    [Fact]
    public async Task GetByTwoLetterCodeAsync_WhenNotExists_ShouldReturnNull()
    {
      
        using var context = GetInMemoryDbContext();
        var repository = new CountryRepository(context);
     
        var result = await repository.GetByTwoLetterCodeAsync("XX", CancellationToken.None);

        Assert.Null(result);
    }
    [Fact]
    public async Task GetByTwoLetterCodeAsync_WithMultipleCountries_ReturnsTheMatchingOne()
    {
        using var context = GetInMemoryDbContext();
        var repo = new CountryRepository(context);
        repo.Add(new Country("gr", "grc", "Greece"));
        repo.Add(new Country("us", "usa", "United States"));
        await context.SaveChangesAsync();

        var result = await repo.GetByTwoLetterCodeAsync("US", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("US", result!.TwoLetterCode);
        Assert.Equal("United States", result.CountryName);
    }
}