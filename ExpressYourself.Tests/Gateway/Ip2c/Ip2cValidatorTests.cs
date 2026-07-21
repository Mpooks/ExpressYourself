using Xunit;
using Microsoft.Extensions.Options;
using ExpressYourself.Gateway.Ip2c;

public class Ip2cOptionsValidatorTests
{
    private readonly Ip2cOptionsValidator _validator = new();


    [Fact]
    public void Validate_WhenOptionsAreValid_ReturnsSuccess()
    {
        var options = new Ip2cOptions
        {
            BaseUrl = "https://ip2c.org",
            TimeoutSeconds = 5
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }


    [Fact]
    public void Validate_WhenBaseUrlIsInvalid_ReturnsFail()
    {
        var options = new Ip2cOptions
        {
            BaseUrl = "test-wrong-url-test-",
            TimeoutSeconds = 5
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("BaseUrl", result.FailureMessage);
    }


    [Fact]
    public void Validate_WhenTimeoutIsZero_ReturnsFail()
    {
        var options = new Ip2cOptions
        {
            BaseUrl = "https://ip2c.org",
            TimeoutSeconds = 0
        };

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains("TimeoutSeconds", result.FailureMessage);
    }
}