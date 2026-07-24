namespace ExpressYourself.API.Middleware
{
    public sealed class CorrelationLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationLoggingMiddleware> _logger;


        public CorrelationLoggingMiddleware(RequestDelegate next,ILogger<CorrelationLoggingMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(logger);

            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using (_logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["TraceId"] = context.TraceIdentifier
                }))
            {
                await _next(context);
            }
        }
    }
}
