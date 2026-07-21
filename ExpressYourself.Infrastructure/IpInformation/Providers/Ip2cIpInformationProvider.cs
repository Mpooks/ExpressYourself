using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Gateway.Ip2c;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class Ip2cIpInformationProvider : IIpInformationProvider
    {
        private readonly IIp2cClient _client;
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countries;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _clock;
        private readonly IIpInformationCache _cache;

        public Ip2cIpInformationProvider(IIp2cClient client, IIpAddressRepository ipAddressRepository, ICountryRepository countries, IUnitOfWork unitOfWork, TimeProvider clock, IIpInformationCache cache)
        {
            _client = client;
            _ipAddressRepository = ipAddressRepository;
            _countries = countries;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _cache = cache;
        }

        public async Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            Ip2cLookupResult result =
                await _client.GetIpInformationAsync(address, cancellationToken);

            DateTimeOffset now = _clock.GetUtcNow();

            switch (result.Status)
            {
                case Ip2cLookupStatus.Success:
                    bool successChanged = await PersistSuccessAsync(address, result, now, cancellationToken);

                    if (successChanged)
                    {
                        await _cache.RemoveAsync(address, cancellationToken);
                    }

                    return new IpInformationDto(address, result.TwoLetterCode!, result.ThreeLetterCode!, result.CountryName!);

                case Ip2cLookupStatus.Unknown:
                    bool unknownChanged = await PersistUnknownAsync(address, now, cancellationToken);

                    if (unknownChanged)
                    {
                        await _cache.RemoveAsync(address, cancellationToken);
                    }

                    throw new UnknownIpAddressException(address);

                case Ip2cLookupStatus.Invalid:
                default:
                    throw new InvalidIpAddressException("IP2C rejected this ip address as invalid", address);
            }
        }

        private async Task<bool> PersistSuccessAsync(string address, Ip2cLookupResult result, DateTimeOffset now, CancellationToken cancellationToken)
        {
            IpAddress? ip = await _ipAddressRepository.GetByAddressAsync(address, cancellationToken);

            if (ip is null)
            {
                ip = new IpAddress(address);
                _ipAddressRepository.Add(ip);
            }

            bool ipInformationChanged = ip.SetCountry(result.TwoLetterCode!, now);

            Country? country = await _countries.GetByTwoLetterCodeAsync(result.TwoLetterCode!, cancellationToken);

            if (country is null)
            {
                _countries.Add(new Country(result.TwoLetterCode!, result.ThreeLetterCode!, result.CountryName!));
            }
            else
            {
                country.UpdateInformation(result.ThreeLetterCode!, result.CountryName!);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ipInformationChanged;
        }

        private async Task<bool> PersistUnknownAsync(string address, DateTimeOffset now, CancellationToken cancellationToken)
        {
            IpAddress? ip = await _ipAddressRepository.GetByAddressAsync(address, cancellationToken);

            if (ip is null)
            {
                ip = new IpAddress(address);
                _ipAddressRepository.Add(ip);
            }

            bool ipInformationChanged = ip.MarkAsUnknown(now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ipInformationChanged;
        }
    }
}