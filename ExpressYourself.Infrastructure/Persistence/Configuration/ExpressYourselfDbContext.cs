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
        modelBuilder.Entity<IpAddress>(entity =>
        {
            entity.ToTable("IpAddress");
            entity.HasKey(w => w.Address);
            entity.Property(w => w.Status).HasConversion<string>();
            entity.Property(w => w.CountryTwoLetterCode).HasConversion<string>().IsRequired();
        });
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Country");
            entity.HasKey(w => w.TwoLetterCode);
            entity.Property(w => w.ThreeLetterCode).HasConversion<string>().IsRequired();
            entity.Property(w => w.CountryName).HasConversion<string>().IsRequired();
        });

        
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}