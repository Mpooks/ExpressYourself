using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Xunit;

namespace ExpressYourself.Tests.Infrastructure.Persistence;

public sealed class SchemaConstraintTests
{
    [Fact]
    public async Task ThreeLetterCode_UniqueIndex_IsEnforced()
    {
        using var connection = new SqliteConnection("DataSource =:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ExpressYourselfDbContext>()
            .UseSqlite(connection).Options;

        using var context = new ExpressYourselfDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.Countries.Add(new Country("gr", "xyz", "Greece"));
        context.Countries.Add(new Country("de", "xyz", "Germany"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}