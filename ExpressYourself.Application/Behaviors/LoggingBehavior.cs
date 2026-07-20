using MediatR;
using Microsoft.Extensions.Logging;

namespace ExpressYourself.Application.Behaviors
{
    public class LoggingBehavior<TRequest , TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest req, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Processing request {typeof(TRequest).Name}");
            TResponse response = await next();
            _logger.LogInformation("Completed request {RequestType} with response type {ResponseType}",typeof(TRequest).Name,typeof(TResponse).Name);
            return response;
        }
    }
}
