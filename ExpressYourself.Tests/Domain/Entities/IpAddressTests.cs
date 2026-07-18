using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Enums;
using ExpressYourself.Domain.Exceptions;
using Xunit;

namespace ExpressYourself.Tests.Domain.Entities;

public sealed class IpAddressTests
{
    private static readonly DateTimeOffset FirstUpdate =
        new(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset SecondUpdate =
        FirstUpdate.AddHours(1);

    [Fact]
    public void Constructor_ValidIPv4_InitializesInitialState()
    {
        // Arrange
        string address = "1.2.3.4";

        // Act
        var ipAddress = new IpAddress(address);

        // Assert
        Assert.Equal("1.2.3.4", ipAddress.Address);
        Assert.Equal(IpStatus.Pending, ipAddress.Status);
        Assert.Null(ipAddress.CountryTwoLetterCode);
        Assert.Null(ipAddress.LastUpdated);
    }

    [Fact]
    public void Constructor_AddressWithWhitespace_TrimsToNormalizedAddress()
    {
        // Arrange
        string address = "  1.2.3.4  ";

        // Act
        var ipAddress = new IpAddress(address);

        // Assert
        Assert.Equal("1.2.3.4", ipAddress.Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-ip")]
    [InlineData("999.999.999.999")]
    [InlineData("2001:db8::1")]
    public void Constructor_InvalidAddress_ThrowsDomainValidationException(
        string address)
    {
        // Act
        Action action = () => new IpAddress(address);

        // Assert
        Assert.Throws<DomainValidationException>(action);
    }

    [Fact]
    public void SetCountry_ValidCountryCode_SetsCodeAndSuccessStatus()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        // Act
        ipAddress.SetCountry("gr", FirstUpdate);

        // Assert
        Assert.Equal("GR", ipAddress.CountryTwoLetterCode);
        Assert.Equal(IpStatus.Success, ipAddress.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("G")]
    [InlineData("GRC")]
    [InlineData("12")]
    public void SetCountry_InvalidCountryCode_ThrowsDomainValidationException(
        string countryCode)
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        // Act
        Action action = () =>
            ipAddress.SetCountry(countryCode, FirstUpdate);

        // Assert
        Assert.Throws<DomainValidationException>(action);
    }

    [Fact]
    public void SetCountry_NonUtcTimestamp_NormalizesTimestampToUtc()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        DateTimeOffset localTime =
            new(
                2026,
                7,
                18,
                13,
                0,
                0,
                TimeSpan.FromHours(3));

        DateTimeOffset expectedUtc =
            localTime.ToUniversalTime();

        // Act
        bool hasChanged =
            ipAddress.SetCountry("GR", localTime);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(expectedUtc, ipAddress.LastUpdated);
        Assert.Equal(TimeSpan.Zero, ipAddress.LastUpdated!.Value.Offset);
    }

    [Fact]
    public void SetCountry_FirstCall_ReturnsTrueAndSetsLastUpdated()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        // Act
        bool hasChanged =
            ipAddress.SetCountry("GR", FirstUpdate);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(FirstUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void SetCountry_SameCountry_ReturnsFalseAndKeepsLastUpdated()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        ipAddress.SetCountry(
            "GR",
            FirstUpdate);

        // Act
        bool hasChanged =
            ipAddress.SetCountry(
                "gr",
                SecondUpdate);

        // Assert
        Assert.False(hasChanged);
        Assert.Equal(FirstUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void SetCountry_DifferentCountry_ReturnsTrueAndUpdatesLastUpdated()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        ipAddress.SetCountry(
            "GR",
            FirstUpdate);

        // Act
        bool hasChanged =
            ipAddress.SetCountry(
                "US",
                SecondUpdate);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal("US", ipAddress.CountryTwoLetterCode);
        Assert.Equal(SecondUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void MarkAsUnknown_FirstCall_SetsUnknownAndClearsCountry()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        // Act
        bool hasChanged =
            ipAddress.MarkAsUnknown(FirstUpdate);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(IpStatus.UnknownIp, ipAddress.Status);
        Assert.Null(ipAddress.CountryTwoLetterCode);
        Assert.Equal(FirstUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void MarkAsUnknown_NonUtcTimestamp_NormalizesTimestampToUtc()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        DateTimeOffset localTime =
            new(
                2026,
                7,
                18,
                13,
                0,
                0,
                TimeSpan.FromHours(3));

        DateTimeOffset expectedUtc =
            localTime.ToUniversalTime();

        // Act
        bool hasChanged =
            ipAddress.MarkAsUnknown(localTime);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(expectedUtc, ipAddress.LastUpdated);
        Assert.Equal(TimeSpan.Zero, ipAddress.LastUpdated!.Value.Offset);
    }

    [Fact]
    public void MarkAsUnknown_AlreadyUnknown_ReturnsFalseAndKeepsLastUpdated()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        ipAddress.MarkAsUnknown(FirstUpdate);

        // Act
        bool hasChanged =
            ipAddress.MarkAsUnknown(SecondUpdate);

        // Assert
        Assert.False(hasChanged);
        Assert.Equal(FirstUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void MarkAsUnknown_AfterSuccess_ReturnsTrueAndClearsCountry()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        ipAddress.SetCountry(
            "GR",
            FirstUpdate);

        // Act
        bool hasChanged =
            ipAddress.MarkAsUnknown(SecondUpdate);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(IpStatus.UnknownIp, ipAddress.Status);
        Assert.Null(ipAddress.CountryTwoLetterCode);
        Assert.Equal(SecondUpdate, ipAddress.LastUpdated);
    }

    [Fact]
    public void SetCountry_AfterUnknown_ReturnsTrueAndSetsCountry()
    {
        // Arrange
        var ipAddress = new IpAddress("1.2.3.4");

        ipAddress.MarkAsUnknown(FirstUpdate);

        // Act
        bool hasChanged =
            ipAddress.SetCountry(
                "GR",
                SecondUpdate);

        // Assert
        Assert.True(hasChanged);
        Assert.Equal(IpStatus.Success, ipAddress.Status);
        Assert.Equal("GR", ipAddress.CountryTwoLetterCode);
        Assert.Equal(SecondUpdate, ipAddress.LastUpdated);
    }
}