using ExpressYourself.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpressYourself.Infrastructure.Persistence.Configurations;

internal sealed class IpAddressConfiguration : IEntityTypeConfiguration<IpAddress>
{
    public void Configure(EntityTypeBuilder<IpAddress> builder)
    {
        builder.ToTable("IpAddresses");

        builder.HasKey(i => i.Address);
        builder.Property(i => i.Address).HasMaxLength(15).IsRequired();

        builder.Property(i => i.CountryTwoLetterCode).HasMaxLength(2).IsFixedLength().IsRequired(false);

        builder.Property(i => i.Status).HasConversion<int>().IsRequired();

        builder.Property(i => i.LastUpdated).IsRequired(false);

        builder.HasOne<Country>()
        .WithMany()
        .HasForeignKey(i => i.CountryTwoLetterCode)
        .OnDelete(DeleteBehavior.SetNull);

        builder.Property<byte[]>("RowVersion").IsRowVersion();
    }
}
