using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Exceptions;

namespace ExpressYourself.Tests.Domain.Entities;

public sealed class CountryTests
{
    [Fact]
    public void Constructor_ValidValues_NormalizesProperties()
    {
        var country = new Country(
            "gr",
            "grc",
            "  Greece  ");

        Assert.Equal("GR", country.TwoLetterCode);
        Assert.Equal("GRC", country.ThreeLetterCode);
        Assert.Equal("Greece", country.CountryName);
    }

    [Fact]
    public void UpdateInformation_SameValues_ReturnsFalse()
    {
        var country = new Country(
            "GR",
            "GRC",
            "Greece");

        bool hasChanged = country.UpdateInformation(
            "grc",
            " Greece ");

        Assert.False(hasChanged);
    }

    [Fact]
    public void UpdateInformation_DifferentValues_UpdatesCountry()
    {
        var country = new Country(
            "GR",
            "GRC",
            "Greece");

        bool hasChanged = country.UpdateInformation(
            "GRE",
            "Hellenic Republic");

        Assert.True(hasChanged);
        Assert.Equal("GRE", country.ThreeLetterCode);
        Assert.Equal(
            "Hellenic Republic",
            country.CountryName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("G")]
    [InlineData("GRC")]
    public void Constructor_InvalidTwoLetterCode_ThrowsException(
        string twoLetterCode)
    {
        Assert.Throws<DomainValidationException>(
            () => new Country(
                twoLetterCode,
                "GRC",
                "Greece"));
    }
}