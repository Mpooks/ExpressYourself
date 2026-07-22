using ExpressYourself.API.Controllers;
using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.CountryReports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ExpressYourself.Tests.Api;

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
}