using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ExpressYourself.Application.Behaviors
{
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> Handle(TRequest req, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                return await next();
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation($"Request {typeof(TRequest).Name} took {stopwatch.ElapsedMilliseconds}");
            }
        }
    }
}
