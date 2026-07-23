using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ExpressYourself.Application.Features.IpRefresh.Commands
{
    public sealed class RefreshStoredIpsCommandHandler
        : IRequestHandler<RefreshStoredIpsCommand, RefreshStoredIpsResult>
    {
        // TEMP: hard-coded for now. Become configurable (RefreshJobOptions.BatchSize /
        // MaxConcurrency) when we wire options in the Quartz commit.
        private const int BatchSize = 100;
        private const int MaxConcurrency = 4;

        private readonly IIp2cClient _ip2cClient;
        private readonly IIpAddressRepository _ipAddressRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IIpInformationCache _cache;
        private readonly TimeProvider _clock;
        private readonly ILogger<RefreshStoredIpsCommandHandler> _logger;

        public RefreshStoredIpsCommandHandler(
            IIp2cClient ip2cClient,
            IIpAddressRepository ipAddressRepository,
            ICountryRepository countryRepository,
            IUnitOfWork unitOfWork,
            IIpInformationCache cache,
            TimeProvider clock,
            ILogger<RefreshStoredIpsCommandHandler> logger)
        {
            _ip2cClient = ip2cClient;
            _ipAddressRepository = ipAddressRepository;
            _countryRepository = countryRepository;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _clock = clock;
            _logger = logger;
        }

        public async Task<RefreshStoredIpsResult> Handle(
            RefreshStoredIpsCommand request,
            CancellationToken cancellationToken)
        {
            int scanned = 0;
            int changed = 0;
            int unchanged = 0;
            int failed = 0;

            DateTimeOffset now = _clock.GetUtcNow();
            string? afterAddress = null;

            while (true)
            {
                IReadOnlyList<IpAddress> batch =
                    await _ipAddressRepository.GetBatchAsync(afterAddress, BatchSize, cancellationToken);

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
                        _logger.LogWarning(lookup.Error, "Failed to look up IP {Address}.", lookup.Address);
                        continue;
                    }

                    try
                    {
                        RefreshOutcome outcome =
                            await ApplyAsync(lookup.Address, lookup.Result!, now, cancellationToken);

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
                        _logger.LogWarning(exception, "Failed to persist IP {Address}.", lookup.Address);
                    }
                }

                _unitOfWork.ClearTracked();

                afterAddress = batch[^1].Address;

                if (batch.Count < BatchSize)
                {
                    break;
                }
            }

            return new RefreshStoredIpsResult(scanned, changed, unchanged, failed);
        }

        private async Task<BatchLookup[]> LookupBatchAsync(
            IReadOnlyList<IpAddress> batch,
            CancellationToken cancellationToken)
        {
            var lookups = new BatchLookup[batch.Count];

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrency,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync( Enumerable.Range(0, batch.Count), options, async (index, token) =>
            {
                string address = batch[index].Address;

                try
                {
                    Ip2cLookupResult result = await _ip2cClient.GetIpInformationAsync(address, token);

                    lookups[index] = new BatchLookup(address, result, null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    lookups[index] = new BatchLookup(address, null, exception);
                }
            });

            return lookups;
        }

        private async Task<RefreshOutcome> ApplyAsync(
            string address,
            Ip2cLookupResult lookup,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            IpAddress? ip = await _ipAddressRepository.GetByAddressAsync(address, cancellationToken);

            if (ip is null)
            {
                return RefreshOutcome.Failed;
            }

            bool ipChanged;
            bool countryChanged = false;
            string? countryCode = null;

            switch (lookup.Status)
            {
                case Ip2cLookupStatus.Success:
                    ipChanged = ip.SetCountry(lookup.TwoLetterCode!, now);
                    countryChanged = await EnsureCountryAsync(lookup, cancellationToken);
                    countryCode = lookup.TwoLetterCode!;
                    break;

                case Ip2cLookupStatus.Unknown:
                    ipChanged = ip.MarkAsUnknown(now);
                    break;

                default:
                    return RefreshOutcome.Failed;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateCacheAsync(address, ipChanged, countryChanged, countryCode, cancellationToken);

            return ipChanged ? RefreshOutcome.Changed : RefreshOutcome.Unchanged;
        }

        private async Task<bool> EnsureCountryAsync(
            Ip2cLookupResult lookup,
            CancellationToken cancellationToken)
        {
            Country? country = await _countryRepository.GetByTwoLetterCodeAsync(lookup.TwoLetterCode!, cancellationToken);

            if (country is null)
            {
                _countryRepository.Add(new Country(lookup.TwoLetterCode!, lookup.ThreeLetterCode!, lookup.CountryName!));
                return false;
            }

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

        private sealed record BatchLookup(string Address, Ip2cLookupResult? Result, Exception? Error);
    }
}