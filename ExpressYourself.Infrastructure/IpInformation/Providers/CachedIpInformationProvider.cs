using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Domain.Validation;
using ExpressYourself.Gateway.Ip2c;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class CachedIpInformationProvider : IIpInformationProvider
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

        private readonly IIpInformationProvider _inner;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl;

        public CachedIpInformationProvider(IIpInformationProvider inner, IMemoryCache cache, TimeSpan ttl)
        {
            _inner = inner;
            _cache = cache;
            _ttl = ttl;
        }

        public async Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            string key = BuildKey(address);

            if (TryRead(key,address,out IpInformationDto? hit))
            {
                return hit!;
            }
            SemaphoreSlim gate = Gates.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                if (TryRead(key,address,out IpInformationDto? filledDto))
                {
                    return filledDto!;
                }
                var dto = await _inner.GetIpInformationAsync(address, cancellationToken);
                _cache.Set(
                    key,
                    new IpInformationCacheEntry(dto.CountryName, dto.TwoLetterCountryCode, dto.ThreeLetterCountryCode),
                    _ttl);
                return dto;
            }
            finally
            {
                gate.Release();
                if (gate.CurrentCount == 1)
                {
                    Gates.TryRemove(new KeyValuePair<string, SemaphoreSlim>(key, gate));
                }
            }
        }

        private bool TryRead(string key, string address, out IpInformationDto? dto)
        {
            if (_cache.TryGetValue(key, out IpInformationCacheEntry? entry) && entry is not null)
            {
                dto = new IpInformationDto(address, entry.TwoLetterCode, entry.ThreeLetterCode, entry.CountryName);
                return true;
            }
            dto = null;
            return false;
        }

        private static string BuildKey(string address) => $"ip-info:{IpAddressValidator.Normalize(address)}";
    }
}
