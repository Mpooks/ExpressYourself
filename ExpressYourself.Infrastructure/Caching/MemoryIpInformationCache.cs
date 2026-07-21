using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExpressYourself.Infrastructure.Caching
{
    internal sealed class MemoryIpInformationCache : IIpInformationCache
    {
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _ttl;

        public MemoryIpInformationCache(IMemoryCache cache, IOptions<CacheOptions> options)
        {
            ArgumentNullException.ThrowIfNull(cache);
            ArgumentNullException.ThrowIfNull(options);

            _cache = cache;
            _ttl = TimeSpan.FromMinutes(options.Value.DefaultTtlMinutes);

        }

        public Task<IpInformationCacheEntry?> GetAsync(string address, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string key = IpInformationCacheKeys.ForAddress(address);

            _cache.TryGetValue(key, out IpInformationCacheEntry? entry);

            return Task.FromResult(entry);
        }

        public Task SetAsync(string address, IpInformationCacheEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(entry);

            string key = IpInformationCacheKeys.ForAddress(address);

            _cache.Set(key, entry, _ttl);

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string address, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string key = IpInformationCacheKeys.ForAddress(address);

            _cache.Remove(key);

            return Task.CompletedTask;
        }
    }
}
