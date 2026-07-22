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
                throw new Ip2cResponseFormatException("Response cannot be null or empty.");
            }

            var responseParts = rawResponse.Split(';');
            if (responseParts.Length != 4)
            {
                throw new Ip2cResponseFormatException("Invalid IP2C response. Format must be [Status, TwoLetterCode, ThreeLetterCode, CountryName].");
            }

            var status = responseParts[0] switch
            {
                "0" => Ip2cLookupStatus.Invalid,
                "1" => Ip2cLookupStatus.Success,
                "2" => Ip2cLookupStatus.Unknown,
                _ => throw new Ip2cResponseFormatException($"Unknown status '{responseParts[0]}'.")
            };

            string twoLetterCode = responseParts[1];
            string threeLetterCode = responseParts[2];
            string countryName = responseParts[3];

            if (status == Ip2cLookupStatus.Success && !IsValidSuccessShape(twoLetterCode, threeLetterCode, countryName))
            {
                throw new Ip2cResponseFormatException("I2PC retuned a malformed success response.");
            }

            return new Ip2cLookupResult(status, twoLetterCode, threeLetterCode, countryName);
        }
            private static bool IsValidSuccessShape(string twoLetterCode, string threeLetterCode, string countryName) =>
            IsLetterCodeOfLength(twoLetterCode, 2) &&
            IsLetterCodeOfLength(threeLetterCode, 3) &&
            !string.IsNullOrWhiteSpace(countryName);

            private static bool IsLetterCodeOfLength(string value, int length) =>
            value.Length == length && value.All(char.IsAsciiLetter);


    }
}
