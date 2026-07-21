using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class DatabaseIpInformationProvider : IIpInformationProvider
    {
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IIpInformationProvider _inner;
        private readonly ILogger<DatabaseIpInformationProvider> _logger;

        public DatabaseIpInformationProvider(
            IIpAddressRepository ipAddressRepository, 
            ICountryRepository countryRepository, 
            IIpInformationProvider inner,
            ILogger<DatabaseIpInformationProvider> logger)
        {
            _ipAddressRepository = ipAddressRepository;
            _countryRepository = countryRepository;
            _inner = inner;
            _logger = logger;
        }

        public async Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            var ip = await _ipAddressRepository.GetByAddressAsync(address, cancellationToken);

            if (ip is not null && ip.Status is IpStatus.Success)
            {
                var country = await _countryRepository.GetByTwoLetterCodeAsync(ip.CountryTwoLetterCode!,cancellationToken);

                if (country is not null)
                {
                    return new IpInformationDto(address, country.TwoLetterCode, country.ThreeLetterCode, country.CountryName);
                }
                _logger.LogWarning(
                    "IP {Address} stored as Success with code {Code} but no matching Country row; falling back to external lookup.",
                    address, ip.CountryTwoLetterCode);
            }
            if (ip is not null && ip.Status is IpStatus.UnknownIp)
            {
                throw new UnknownIpAddressException(address);
            }

            return await _inner.GetIpInformationAsync(address, cancellationToken);
        }
    }
}
