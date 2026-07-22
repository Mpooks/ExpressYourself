using ExpressYourself.API.ExceptionHandler;
using ExpressYourself.Application.Errors;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Gateway.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExpressYourself.Tests.Api.UnitTests
{

    public class ApiExceptionHandlerTests
    {
        [Theory]
        [InlineData(400)]
        [InlineData(404)]
        [InlineData(502)]
        [InlineData(503)]
        [InlineData(500)]
        public async Task TryHandleAsync_MapsException_ToExpectedStatusCode(int expectedStatus)
        {
            // Arrange
            Exception ex = expectedStatus switch
            {
                400 => new InvalidIpAddressException("bad", "x"),
                404 => new UnknownIpAddressException("x"),
                502 => new Ip2cResponseFormatException("malformed"),
                503 => new Ip2cUnavailableException("down"),
                _ => new Exception()
            };
            var problemDetails = new Mock<IProblemDetailsService>();
            problemDetails.Setup(p => p.TryWriteAsync(It.IsAny<ProblemDetailsContext>())).ReturnsAsync(true);
            var handler = new ApiExceptionHandler(problemDetails.Object, NullLogger<ApiExceptionHandler>.Instance);
            var context = new DefaultHttpContext();
            // Act
            await handler.TryHandleAsync(context, ex, CancellationToken.None);
            // Assert
            Assert.Equal(expectedStatus, context.Response.StatusCode);
        }
    }
    
}