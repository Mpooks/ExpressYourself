using ExpressYourself.Domain.Exceptions;
using System.Net;
using System.Net.Sockets;

namespace ExpressYourself.Domain.Validation;

public static class IpAddressValidator
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                "IP address is required.");
        }

        string trimmedAddress = value.Trim();

        var parts = trimmedAddress.Split('.');
        if (parts.Length != 4)
        {
            throw new DomainValidationException(
                "A valid IPv4 address is required.");
        }

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out var octet) || octet < 0 || octet > 255)
            {
                throw new DomainValidationException(
                    "A valid IPv4 address is required.");
            }
        }

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