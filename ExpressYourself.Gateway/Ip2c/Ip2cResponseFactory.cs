using ExpressYourself.Application.Interfaces;
using ExpressYourself.Gateway.Exceptions;

namespace ExpressYourself.Gateway.Ip2c
{
    public static class Ip2cResponseFactory
    {
        public static Ip2cLookupResult Create(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
               throw new Ip2cResponseFormatException(nameof(rawResponse));
            }

            var responseParts=rawResponse.Split(';');
            if (responseParts.Length != 4) 
            {
                throw new Ip2cResponseFormatException("Invalid IP2C reponse. Format must be [Status, TwoLetterCode, ThreeLetterCode, CountryName].");
            }

            var status = responseParts[0] switch
            {
                "1" => Ip2cLookupStatus.Success,
                "0" => Ip2cLookupStatus.Unknown,
                "2" => Ip2cLookupStatus.Invalid,
                _ => throw new Ip2cResponseFormatException($"Unknown status '{responseParts[0]}'.")
            };

            return new Ip2cLookupResult(status, responseParts[1], responseParts[2], responseParts[3]);
        }

    }
}
