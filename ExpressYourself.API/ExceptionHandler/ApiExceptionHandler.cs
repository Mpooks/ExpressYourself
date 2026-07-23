using ExpressYourself.Application.Exceptions;
using ExpressYourself.Gateway.Exceptions;
using Microsoft.AspNetCore.Diagnostics;


namespace ExpressYourself.API.ExceptionHandler
{
    public sealed class ApiExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<ApiExceptionHandler> _logger;
        public ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception ex, CancellationToken cancellationToken)
        {
            (int status, string title) = ex switch
            {
                InvalidIpAddressException => (StatusCodes.Status400BadRequest, "The IP address is invalid"),
                UnknownIpAddressException => (StatusCodes.Status404NotFound, "The IP address was not found"),
                Ip2cResponseFormatException => (StatusCodes.Status502BadGateway, "Invalid upstream response"),
                Ip2cUnavailableException => (StatusCodes.Status503ServiceUnavailable, "The service is unavailable"),
                InvalidCountryCodeException => (StatusCodes.Status400BadRequest, "Invalid country code"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occured")
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception");
            }
            else
            {
                _logger.LogWarning(ex, "The request failed with status {StatusCode}", status);
            }

            httpContext.Response.StatusCode = status;
            
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {HttpContext = httpContext, Exception = ex, ProblemDetails =
            {Status = status,Title = title, Detail = status == StatusCodes.Status500InternalServerError? "An unexpected error occurred." : ex.Message}});
        }
    }
}
