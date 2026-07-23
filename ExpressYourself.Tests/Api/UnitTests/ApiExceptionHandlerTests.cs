using ExpressYourself.API.ExceptionHandler;
using ExpressYourself.Application.Exceptions;
using ExpressYourself.Gateway.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExpressYourself.Tests.Api.UnitTests
{

    public class ApiExceptionHandlerTests
    {
        public static IEnumerable<object[]> ExceptionCases()
        {
            yield return new object[] { new InvalidIpAddressException("bad", "x"), 400 };
            yield return new object[] { new InvalidCountryCodeException("bad"), 400 };
            yield return new object[] { new UnknownIpAddressException("x"), 404 };
            yield return new object[] { new Ip2cResponseFormatException("bad"), 502 };
            yield return new object[] { new Ip2cUnavailableException("down"), 503 };
            yield return new object[] { new Exception(), 500 };
        }

        [Theory]
        [MemberData(nameof(ExceptionCases))]
        public async Task TryHandleAsync_MapsException_ToExpectedStatusCode(Exception ex, int expectedStatus)
        {
            // Arrange
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