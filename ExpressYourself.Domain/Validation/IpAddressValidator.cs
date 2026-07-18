using ExpressYourself.Domain.Exceptions;
using System.Net;
using System.Net.Sockets;

namespace ExpressYourself.Domain.Validation;

internal static class IpAddressValidator
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "IP address is required.");
        }

        string trimmedAddress = value.Trim();

        if (!IPAddress.TryParse(
                trimmedAddress,
                out IPAddress? parsedAddress) || parsedAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new DomainValidationException(
                "A valid IPv4 address is required.");
        }

        return parsedAddress.ToString();
    }
}