using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Validation;
using ExpressYourself.Gateway.Exceptions;

namespace ExpressYourself.Gateway.Ip2c;

public sealed class Ip2cClient : IIp2cClient
{
    private readonly HttpClient _httpClient;

    public Ip2cClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Ip2cLookupResult> GetIpInformationAsync(string address, CancellationToken cancellationToken)
    {
        string normalizedAddress = IpAddressValidator.Normalize(address);

        using HttpResponseMessage response = await _httpClient.GetAsync(normalizedAddress, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new Ip2cUnavailableException($"IP2C returned HTTP status code {(int)response.StatusCode}.");
        }
        string rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        return Ip2cResponseFactory.Create(rawResponse);
    }
}

        


