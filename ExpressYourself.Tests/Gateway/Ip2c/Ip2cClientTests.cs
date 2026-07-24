using ExpressYourself.Application.Interfaces;
using ExpressYourself.Gateway.Exceptions;
using ExpressYourself.Gateway.Ip2c;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly.Timeout;
using System.Net;

namespace ExpressYourself.Tests.Gateway.Ip2c;

public class Ip2cClientTests
{
    [Fact]
    public async Task GetIpInformationAsync_WhenResponseIsSuccessful_ReturnsResult()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("1;GR;GRC;Greece")
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://ip2c.org/")
        };

        var logger = new Mock<ILogger<Ip2cClient>>();

        var client = new Ip2cClient(
            httpClient,
            logger.Object);

        var result = await client.GetIpInformationAsync(
            "8.8.8.8",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(Ip2cLookupStatus.Success, result.Status);
    }


    [Fact]
    public async Task GetIpInformationAsync_WhenResponseIsNotSuccessful_ThrowsException()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://ip2c.org/")
        };

        var logger = new Mock<ILogger<Ip2cClient>>();

        var client = new Ip2cClient(
            httpClient,
            logger.Object);

        await Assert.ThrowsAsync<Ip2cUnavailableException>(() =>
            client.GetIpInformationAsync(
                "8.8.8.8",
                CancellationToken.None));
    }

    [Fact]
    public async Task GetIpInformationAsync_WhenHttpRequestFails_ThrowsIp2cUnavailableException()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://ip2c.org/")
        };

        var logger = new Mock<ILogger<Ip2cClient>>();

        var client = new Ip2cClient(
            httpClient,
            logger.Object);

        Func<Task> action = () => client.GetIpInformationAsync("8.8.8.8", CancellationToken.None);

        await Assert.ThrowsAsync<Ip2cUnavailableException>(action);
    }
    [Fact]
    public async Task GetIpInformationAsync_WhenRequestSucceeds_LogsLookupLatency()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("1;GR;GRC;Greece")
            });

        var logger = new Mock<ILogger<Ip2cClient>>();

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        };

        var client = new Ip2cClient(
            httpClient,
            logger.Object);

        await client.GetIpInformationAsync(
            "8.8.8.8",
            CancellationToken.None);

        logger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains(
                        "IP2C lookup for 8.8.8.8 completed in")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetIpInformationAsync_WhenRequestFails_StillLogsLookupLatency()
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var logger = new Mock<ILogger<Ip2cClient>>();

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        };

        var client = new Ip2cClient(
            httpClient,
            logger.Object);

        await Assert.ThrowsAsync<Ip2cUnavailableException>(() =>
            client.GetIpInformationAsync(
                "8.8.8.8",
                CancellationToken.None));

        logger.Verify(
        log => log.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((state, _) =>
            state.ToString()!.Contains("IP2C lookup") &&
            state.ToString()!.Contains("8.8.8.8") &&
            state.ToString()!.Contains("completed in")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.Once);
    }


}

