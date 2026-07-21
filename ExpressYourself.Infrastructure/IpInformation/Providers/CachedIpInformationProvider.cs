using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Domain.Validation;

namespace ExpressYourself.Infrastructure.IpInformation.Providers
{
    public class CachedIpInformationProvider : IIpInformationProvider
    {
        private static readonly object GatesLock = new();

        private static readonly Dictionary<string, GateEntry> Gates = new(StringComparer.Ordinal);

        private readonly IIpInformationProvider _inner;
        private readonly IIpInformationCache _cache;

        public CachedIpInformationProvider(IIpInformationProvider inner, IIpInformationCache cache)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(cache);

            _inner = inner;
            _cache = cache;
        }

        public async Task<IpInformationDto> GetIpInformationAsync(string address, CancellationToken cancellationToken)
        {
            string normalizedAddress = IpAddressValidator.Normalize(address);

            IpInformationCacheEntry? cachedEntry = await _cache.GetAsync(normalizedAddress, cancellationToken);

            if (cachedEntry is not null)
            {
                return CreateDto(address, cachedEntry);
            }

            GateEntry gate = RentGate(normalizedAddress);

            try
            {
                await gate.Semaphore.WaitAsync(cancellationToken);

                try
                {
                    cachedEntry = await _cache.GetAsync(normalizedAddress, cancellationToken);

                    if (cachedEntry is not null)
                    {
                        return CreateDto(address, cachedEntry);
                    }

                    IpInformationDto result = await _inner.GetIpInformationAsync(address, cancellationToken);

                    var entry = new IpInformationCacheEntry(result.CountryName, result.TwoLetterCountryCode, result.ThreeLetterCountryCode);

                    await _cache.SetAsync(normalizedAddress, entry, cancellationToken);

                    return result;
                }
                finally
                {
                    gate.Semaphore.Release();
                }
            }
            finally
            {
                ReturnGate(normalizedAddress, gate);
            }
        }

        private static IpInformationDto CreateDto(string address, IpInformationCacheEntry entry)
        {
            return new IpInformationDto(address, entry.TwoLetterCode, entry.ThreeLetterCode, entry.CountryName);
        }

        private static GateEntry RentGate(string key)
        {
            lock (GatesLock)
            {
                if (!Gates.TryGetValue(key, out GateEntry? gate))
                {
                    gate = new GateEntry();
                    Gates.Add(key, gate);
                }

                gate.ReferenceCount++;

                return gate;
            }
        }

        private static void ReturnGate(string key, GateEntry gate)
        {
            lock (GatesLock)
            {
                gate.ReferenceCount--;

                if (gate.ReferenceCount == 0)
                {
                    Gates.Remove(key);
                    gate.Semaphore.Dispose();
                }
            }
        }

        private sealed class GateEntry
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);

            public int ReferenceCount { get; set; }
        }
    }
}