using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Validation;
using ExpressYourself.Gateway.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

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

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(normalizedAddress, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new Ip2cUnavailableException("IP2C could not be reached.", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            throw new Ip2cUnavailableException("IP2C request timed out.", ex);
        }
        catch (BrokenCircuitException ex) 
        {
            throw new Ip2cUnavailableException("IP2C circuit breaker is open.", ex);
        }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new Ip2cUnavailableException($"IP2C returned HTTP status code {(int)response.StatusCode}.");
            }
            string rawResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            return Ip2cResponseFactory.Create(rawResponse);
        }

    }
}

        


