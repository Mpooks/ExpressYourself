using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Gateway.Ip2c;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class Ip2cIpInformationProvider : IIpInformationProvider
    {
        private readonly IIp2cClient _client;
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countries;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _clock;

        public Ip2cIpInformationProvider(
            IIp2cClient client,
            IIpAddressRepository ipAddressRepository,
            ICountryRepository countries,
            IUnitOfWork unitOfWork,
            TimeProvider clock)
        {
            _client = client;
            _ipAddressRepository = ipAddressRepository;
            _countries = countries;
            _unitOfWork = unitOfWork;
            _clock = clock;
        }

        public async Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            var result = await _client.GetIpInformationAsync(address, cancellationToken);
            var now = _clock.GetUtcNow();

            switch (result.Status)
            {
                case Gateway.Ip2c.Ip2cLookupStatus.Success:
                    await PersistSuccessAsync(address, result, now, cancellationToken);
                    return new IpInformationDto(address, result.TwoLetterCode!, result.ThreeLetterCode!, result.CountryName!);
                case Gateway.Ip2c.Ip2cLookupStatus.Unknown:
                    await PersistUnknownAsync(address, now,cancellationToken);
                    throw new UnknownIpAddressException(address);
                case Gateway.Ip2c.Ip2cLookupStatus.Invalid:

                default:
                    throw new InvalidIpAddressException("IP2C rejected this ip address as invalid", address);
            }
        }

        private async Task PersistSuccessAsync(string address, Ip2cLookupResult result, DateTimeOffset now, CancellationToken cancellationToken)
        {
            var ip = await _ipAddressRepository.GetByAddressAsync(address, cancellationToken);
            if (ip is null)
            {
                ip = new IpAddress(address);
                _ipAddressRepository.Add(ip);
            }
            ip.SetCountry(result.TwoLetterCode!, now);

            var country = await _countries.GetByTwoLetterCodeAsync(result.TwoLetterCode!, cancellationToken);
            if (country is null)
            {
                _countries.Add(new Country(result.TwoLetterCode!, result.ThreeLetterCode!, result.CountryName!));
            }
            else
            {
                country.UpdateInformation(result.ThreeLetterCode!, result.CountryName!);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task PersistUnknownAsync(string address, DateTimeOffset now, CancellationToken cancellation)
        {
            var ip = await _ipAddressRepository.GetByAddressAsync(address,cancellation);
            if (ip is null)
            {
                ip = new IpAddress(address);
                _ipAddressRepository.Add(ip);
            }
            ip.MarkAsUnknown(now);
            await _unitOfWork.SaveChangesAsync(cancellation);
        }
    }
}
