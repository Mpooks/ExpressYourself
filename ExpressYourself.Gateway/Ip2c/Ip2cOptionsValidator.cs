using Microsoft.Extensions.Options;
using ExpressYourself.Gateway.Ip2c;

namespace ExpressYourself.Gateway.Ip2c
{
    public sealed class Ip2cOptionsValidator : IValidateOptions<Ip2cOptions>
    {
        public ValidateOptionsResult Validate(string? name, Ip2cOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.BaseUrl) || !Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            {
                return ValidateOptionsResult.Fail("Ip2c:BaseUrl must be a valid absolute URL.");
            }
            if (options.TimeoutSeconds <= 0)
                return ValidateOptionsResult.Fail("Ip2c:TimeoutSeconds must be greater than zero.");

            return ValidateOptionsResult.Success;
        }
    }
}
