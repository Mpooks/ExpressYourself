using ExpressYourself.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Reflection.Emit;

namespace ExpressYourself.Infrastructure.Persistence;

public sealed class ExpressYourselfDbContext : DbContext
{
    public ExpressYourselfDbContext(DbContextOptions<ExpressYourselfDbContext> options)
    : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<IpAddress> IpAddresses => Set<IpAddress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}