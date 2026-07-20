using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Queries;

namespace ExpressYourself.Tests.Application.Features.IpInformation;

public sealed class GetIpInformationQueryValidatorTests
{
    private readonly GetIpInformationQueryValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("999.1.1.1")]          // out of range
    [InlineData("1.2.3")]              // partial dotted form
    [InlineData("abc")]                // not numeric
    [InlineData("::1")]                // IPv6 → wrong AddressFamily
    [InlineData("1.2.3.4.5.6.7.8.9")]  // exceeds max length → caught by length guard
    public void ValidateAndThrow_InvalidInput_Throws(string? input)
    {
        var query = new GetIpInformationQuery(input!);
        Assert.Throws<InvalidIpAddressException>(() => _validator.Validate(query));
    }

    [Theory]
    [InlineData("1.2.3.4")]
    [InlineData("255.255.255.255")]
    [InlineData("  8.8.8.8  ")]        // surrounding whitespace tolerated
    public void ValidateAndThrow_ValidIpv4_DoesNotThrow(string input)
    {
        var query = new GetIpInformationQuery(input);
        var ex = Record.Exception(() => _validator.Validate(query));
        Assert.Null(ex);
    }
}