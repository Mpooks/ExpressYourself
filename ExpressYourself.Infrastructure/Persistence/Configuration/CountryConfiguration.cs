using ExpressYourself.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpressYourself.Infrastructure.Persistence.Configurations;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");

        builder.HasKey(c => c.TwoLetterCode);

        builder.Property(c => c.TwoLetterCode).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.Property(c => c.ThreeLetterCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(c => c.CountryName).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.ThreeLetterCode).IsUnique();
    }
}
