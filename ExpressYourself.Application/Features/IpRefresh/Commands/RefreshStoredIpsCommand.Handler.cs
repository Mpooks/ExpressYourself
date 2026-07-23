using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Configuration;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpressYourself.Application.Features.IpRefresh.Commands
{
    public sealed class RefreshStoredIpsCommandHandler : IRequestHandler<RefreshStoredIpsCommand, RefreshStoredIpsResult>
    {
        private readonly IIp2cClient _ip2cClient;
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIpInformationCache _cache;
        private readonly RefreshJobOptions _options;
        private readonly TimeProvider _clock;
        private readonly ILogger<RefreshStoredIpsCommandHandler> _logger;

        public RefreshStoredIpsCommandHandler(
            IIp2cClient ip2cClient,
            IIpAddressRepository ipAddressRepository,
            ICountryRepository countryRepository,
            IUnitOfWork unitOfWork,
            IIpInformationCache cache,
            IOptions<RefreshJobOptions> options,
            TimeProvider clock,
            ILogger<RefreshStoredIpsCommandHandler> logger)
        {
            _ip2cClient = ip2cClient;
            _ipAddressRepository = ipAddressRepository;
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _options = options.Value;
            _clock = clock;
            _logger = logger;
        }

        public async Task<RefreshStoredIpsResult> Handle(RefreshStoredIpsCommand request, CancellationToken cancellationToken)
        {
            int scanned = 0;
            int changed = 0;
            int unchanged = 0;
            int failed = 0;

            string? afterAddress = null;

            var countryCache = new Dictionary<string, Country>(StringComparer.OrdinalIgnoreCase);

            void ResetTracking()
            {
                _unitOfWork.ClearTracked();
                countryCache.Clear();
            }

            while (true)
            {
                IReadOnlyList<IpAddress> batch = await _ipAddressRepository.GetBatchAsync(afterAddress, _options.BatchSize, cancellationToken);

                if (batch.Count == 0)
                {
                    break;
                }

                BatchLookup[] lookups = await LookupBatchAsync(batch, cancellationToken);

                foreach (BatchLookup lookup in lookups)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    scanned++;

                    if (lookup.Error is not null)
                    {
                        failed++;
                        _logger.LogWarning(lookup.Error, "Failed to look up IP {Address}.", lookup.Ip.Address);
                        continue;
                    }

                    try
                    {
                        RefreshOutcome outcome = await ApplyAsync(lookup.Ip, lookup.Result!, countryCache, cancellationToken);

                        if (outcome == RefreshOutcome.Changed)
                        {
                            changed++;
                        }
                        else if (outcome == RefreshOutcome.Unchanged)
                        {
                            unchanged++;
                        }
                        else
                        {
                            failed++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        failed++;
                        _logger.LogWarning(exception, "Failed to persist IP {Address}.", lookup.Ip.Address);

                        ResetTracking();
                    }
                }

                ResetTracking();

                afterAddress = batch[^1].Address;

                if (batch.Count < _options.BatchSize)
                {
                    break;
                }
            }

            return new RefreshStoredIpsResult(scanned, changed, unchanged, failed);
        }

        private async Task<BatchLookup[]> LookupBatchAsync(IReadOnlyList<IpAddress> batch, CancellationToken cancellationToken)
        {
            var lookups = new BatchLookup[batch.Count];

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxConcurrency,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(Enumerable.Range(0, batch.Count), options, async (index, token) =>
            {
                IpAddress ip = batch[index];

                try
                {
                    Ip2cLookupResult result = await _ip2cClient.GetIpInformationAsync(ip.Address, token);
                    lookups[index] = new BatchLookup(ip, result, null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lookups[index] = new BatchLookup(ip, null, exception);
                }
            });

            return lookups;
        }

        private async Task<RefreshOutcome> ApplyAsync(IpAddress ip, Ip2cLookupResult lookup, Dictionary<string, Country> countryCache, CancellationToken cancellationToken)
        {
            DateTimeOffset now = _clock.GetUtcNow();

            bool ipChanged;
            bool countryChanged = false;
            string? countryCode = null;

            switch (lookup.Status)
            {
                case Ip2cLookupStatus.Success:
                    ipChanged = ip.SetCountry(lookup.TwoLetterCode!, now);
                    countryChanged = await EnsureCountryAsync(lookup, countryCache, cancellationToken);
                    countryCode = lookup.TwoLetterCode!;
                    break;

                case Ip2cLookupStatus.Unknown:
                    ipChanged = ip.MarkAsUnknown(now);
                    break;

                default:
                    return RefreshOutcome.Failed;
            }

            _ipAddressRepository.Update(ip);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await InvalidateCacheAsync(ip.Address, ipChanged, countryChanged, countryCode, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Refreshed IP {Address} but failed to invalidate its cache.", ip.Address);
            }

            return ipChanged ? RefreshOutcome.Changed : RefreshOutcome.Unchanged;
        }

        private async Task<bool> EnsureCountryAsync(Ip2cLookupResult lookup, Dictionary<string, Country> countryCache, CancellationToken cancellationToken)
        {
            string twoLetterCode = lookup.TwoLetterCode!;

            if (countryCache.TryGetValue(twoLetterCode, out Country? cached))
            {
                return cached.UpdateInformation(lookup.ThreeLetterCode!, lookup.CountryName!);
            }

            Country? country = await _countryRepository.GetByTwoLetterCodeAsync(twoLetterCode, cancellationToken);

            if (country is null)
            {
                country = new Country(lookup.TwoLetterCode!, lookup.ThreeLetterCode!, lookup.CountryName!);
                _countryRepository.Add(country);
                countryCache[twoLetterCode] = country;
                return false;
            }

            countryCache[twoLetterCode] = country;
            return country.UpdateInformation(lookup.ThreeLetterCode!, lookup.CountryName!);
        }

        private async Task InvalidateCacheAsync(
            string address,
            bool ipChanged,
            bool countryChanged,
            string? countryCode,
            CancellationToken cancellationToken)
        {
            if (countryChanged && countryCode is not null)
            {
                IReadOnlyList<string> addresses = await _ipAddressRepository.GetAddressesByCountryCodeAsync(countryCode, cancellationToken);

                foreach (string cachedAddress in addresses)
                {
                    await _cache.RemoveAsync(cachedAddress, cancellationToken);
                }
            }
            else if (ipChanged)
            {
                await _cache.RemoveAsync(address, cancellationToken);
            }
        }

        private enum RefreshOutcome
        {
            Changed,
            Unchanged,
            Failed
        }

        private sealed record BatchLookup(IpAddress Ip, Ip2cLookupResult? Result, Exception? Error);
    }
}