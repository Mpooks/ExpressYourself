using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Validation;
using ExpressYourself.Gateway.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ExpressYourself.Gateway.Ip2c;

public sealed class Ip2cClient : IIp2cClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Ip2cClient> _logger;

    public Ip2cClient(HttpClient httpClient,ILogger<Ip2cClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Ip2cLookupResult> GetIpInformationAsync(string address, CancellationToken cancellationToken)
    {
        string normalizedAddress = IpAddressValidator.Normalize(address);
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
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
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation("IP2C lookup for {Address} completed in {ElapsedMilliseconds} ms.",normalizedAddress,stopwatch.ElapsedMilliseconds);
        }

    }
}




