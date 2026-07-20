using ExpressYourself.Gateway.Exceptions;
using ExpressYourself.Gateway.Ip2c;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace ExpressYourself.Tests.Gateway.Ip2c;
public class Ip2cClientTests
{
    [Fact]
    public async Task GetIpInformationAsync__WhenResponseIsSuccessful_ReturnsResult()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",ItExpr.IsAny<HttpRequestMessage>(),ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("1;GR;Greece;Athens")
            });

        var client = new Ip2cClient(new HttpClient(handler.Object));

        var result = await client.GetIpInformationAsync(
            "8.8.8.8",
            CancellationToken.None);

        Assert.NotNull(result);
    }


    [Fact]
    public async Task GetIpInformationAsync_WhenResponseIsNotSuccessful_ThrowsException()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected().Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync(new HttpResponseMessage{StatusCode = HttpStatusCode.InternalServerError});

        var client = new Ip2cClient(new HttpClient(handler.Object));

        await Assert.ThrowsAsync<Ip2cUnavailableException>(() =>
            client.GetIpInformationAsync(
                "8.8.8.8",
                CancellationToken.None));
    }

}