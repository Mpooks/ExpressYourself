using ExpressYourself.Application.Features.IpRefresh.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using MediatR;

namespace ExpressYourself.Application.Features.IpRefresh.Commands
{
    public sealed class RefreshStoredIpsCommandHandler
        : IRequestHandler<RefreshStoredIpsCommand, RefreshStoredIpsResult>
    {
        // TEMP: hard-coded for the skeleton. Becomes configurable (RefreshJobOptions.BatchSize)
        // when we wire options in the Quartz commit.
        private const int BatchSize = 100;

        private readonly IIpAddressRepository _ipAddressRepository;

        public RefreshStoredIpsCommandHandler(IIpAddressRepository ipAddressRepository)
        {
            _ipAddressRepository = ipAddressRepository;
        }

        public async Task<RefreshStoredIpsResult> Handle(
            RefreshStoredIpsCommand request,
            CancellationToken cancellationToken)
        {
            int scanned = 0;
            string? afterAddress = null;

            while (true)
            {
                IReadOnlyList<IpAddress> batch =
                    await _ipAddressRepository.GetBatchAsync(afterAddress, BatchSize, cancellationToken);

                if (batch.Count == 0)
                {
                    break;
                }

                foreach (IpAddress ip in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    scanned++;
                    // Per-IP refresh (lookup + persist + invalidate) arrives in Commit 5.
                }

                afterAddress = batch[^1].Address;

                if (batch.Count < BatchSize)
                {
                    break;
                }
            }

            return new RefreshStoredIpsResult(Scanned: scanned, Changed: 0, Unchanged: 0, Failed: 0);
        }
    }
}