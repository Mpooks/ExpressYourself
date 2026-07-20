using ExpressYourself.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpressYourself.Infrastructure.Persistence.Context;

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
        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Countries");
            entity.HasKey(c => c.TwoLetterCode);
            entity.Property(c => c.TwoLetterCode).HasMaxLength(2).IsFixedLength().IsRequired();
            entity.Property(c => c.ThreeLetterCode).HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(c => c.CountryName).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.ThreeLetterCode).IsUnique();
        });

        modelBuilder.Entity<IpAddress>(entity =>
        {
            entity.ToTable("IpAddresses");
            entity.HasKey(i => i.Address);
            entity.Property(i => i.Address).HasMaxLength(15).IsRequired();
            entity.Property(i => i.CountryTwoLetterCode).HasMaxLength(2).IsFixedLength().IsRequired(false);
            entity.Property(i => i.Status).HasConversion<int>().IsRequired();
            entity.Property(i => i.LastUpdated).IsRequired(false);
            entity.HasOne<Country>()
                  .WithMany()
                  .HasForeignKey(i => i.CountryTwoLetterCode)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.Property<byte[]>("RowVersion").IsRowVersion();
        });
    }
}