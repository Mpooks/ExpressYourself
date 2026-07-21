using Xunit;
using ExpressYourself.Gateway.Ip2c;
using ExpressYourself.Gateway.Exceptions;
using ExpressYourself.Application.Interfaces;

public class Ip2cResponseFactoryTests
{
    [Fact]
    public void Create_WhenResponseIsSuccess_ReturnsResult()
    {
        var response = "1;GR;GRC;Greece";

        var result = Ip2cResponseFactory.Create(response);

        Assert.Equal(Ip2cLookupStatus.Success, result.Status);
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("GRC", result.ThreeLetterCode);
        Assert.Equal("Greece", result.CountryName);
    }


    [Fact]
    public void Create_WhenResponseIsEmpty_ThrowsException()
    {
        var response = "";

        Action action = () => Ip2cResponseFactory.Create(response);

        Assert.Throws<Ip2cResponseFormatException>(action);
    }


    [Fact]
    public void Create_WhenStatusIsUnknown_ThrowsException()
    {
        var response = "5;GR;GRC;Greece";

        Action action = () => Ip2cResponseFactory.Create(response);

        Assert.Throws<Ip2cResponseFormatException>(action);
    }


    [Fact]
    public void Create_WhenResponseIsInvalid_ReturnsResult()
    {
        var response = "0;GR;GRC;Greece";

        var result = Ip2cResponseFactory.Create(response);

        Assert.Equal(Ip2cLookupStatus.Invalid, result.Status);
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("GRC", result.ThreeLetterCode);
        Assert.Equal("Greece", result.CountryName);
    }


    [Fact]
    public void Create_WhenResponseIsUnknown_ReturnsResult()
    {
        var response = "2;GR;GRC;Greece";

        var result = Ip2cResponseFactory.Create(response);

        Assert.Equal(Ip2cLookupStatus.Unknown, result.Status);
        Assert.Equal("GR", result.TwoLetterCode);
        Assert.Equal("GRC", result.ThreeLetterCode);
        Assert.Equal("Greece", result.CountryName);
    }
}