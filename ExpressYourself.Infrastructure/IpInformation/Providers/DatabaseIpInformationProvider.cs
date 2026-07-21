using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Enums;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class DatabaseIpInformationProvider : IIpInformationProvider
    {
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IIpInformationProvider _inner;

        public DatabaseIpInformationProvider(
            IIpAddressRepository ipAddressRepository, 
            ICountryRepository countryRepository, 
            IIpInformationProvider inner)
        {
            _ipAddressRepository = ipAddressRepository;
            _countryRepository = countryRepository;
            _inner = inner;
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
            }
            if (ip is not null && ip.Status is IpStatus.UnknownIp)
            {
                throw new UnknownIpAddressException(address);
            }

            return await _inner.GetIpInformationAsync(address, cancellationToken);
        }
    }
}
