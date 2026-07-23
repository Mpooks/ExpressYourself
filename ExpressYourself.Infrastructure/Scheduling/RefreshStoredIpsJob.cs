using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace ExpressYourself.Infrastructure.Scheduling
{
    [DisallowConcurrentExecution]
    public sealed class RefreshStoredIpsJob : IJob
    {
        private readonly ISender _sender;
        private readonly ILogger<RefreshStoredIpsJob> _logger;

        public RefreshStoredIpsJob(ISender sender, ILogger<RefreshStoredIpsJob> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                RefreshStoredIpsResult result =
                    await _sender.Send(new RefreshStoredIpsCommand(), context.CancellationToken);

                _logger.LogInformation(
                    "IP refresh finished. Scanned={Scanned} Changed={Changed} Unchanged={Unchanged} Failed={Failed}.",
                    result.Scanned, result.Changed, result.Unchanged, result.Failed);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("IP refresh was canceled (host shutting down).");
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "IP refresh job failed.");
                throw;
            }
        }
    }
}