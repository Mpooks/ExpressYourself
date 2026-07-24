using ExpressYourself.API.Controllers;
using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.CountryReports.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ExpressYourself.Tests.Api.UnitTests;

public class ReportsControllerTests
{
    [Fact]
    public async Task GetAsync_WhenSenderReturnsReport_ReturnsOkWithRows()
    {
        // Arrange
        IReadOnlyList<CountryReportDto> expected = [new("Greece", 5, DateTimeOffset.UtcNow), new("United States", 12, DateTimeOffset.UtcNow)];
        var mockSender = new Mock<ISender>();
        mockSender.Setup(s => s.Send(It.IsAny<GetCountryReportQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var controller = new ReportsController(mockSender.Object);
        // Act
        ActionResult<IReadOnlyList<CountryReportDto>> result = await controller.GetAsync(new[] { "GR", "US" }, CancellationToken.None);
        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expected, ok.Value);
        mockSender.Verify(s => s.Send(It.Is<GetCountryReportQuery>(q => q.Codes!.SequenceEqual(new[] { "GR", "US" })),It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GetAsync_WhenMoreThanFiftyCodesAreProvided_ReturnsBadRequestWithoutSendingQuery()
    {
        // Arrange
        var mockSender = new Mock<ISender>();
        var controller = new ReportsController(mockSender.Object);

        string[] codes = Enumerable
            .Range(1, 51)
            .Select(number => $"C{number}")
            .ToArray();

        // Act
        ActionResult<IReadOnlyList<CountryReportDto>> result =
            await controller.GetAsync(
                codes,
                CancellationToken.None);

        // Assert
        var badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        var problemDetails =
            Assert.IsType<ProblemDetails>(badRequest.Value);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            problemDetails.Status);

        Assert.Equal(
            "Too many country codes.",
            problemDetails.Title);

        Assert.Equal(
            "A maximum of 50 country codes is allowed.",
            problemDetails.Detail);

        mockSender.Verify(
            sender => sender.Send(
                It.IsAny<GetCountryReportQuery>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }


}