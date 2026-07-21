using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.CountryReports.Queries;
using ExpressYourself.Application.Infrastructure.Persistence;
using Moq;

namespace ExpressYourself.Tests.Application.Features.CountryReports;

public sealed class GetCountryReportQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidInformation_ReturnsReportFromRepository()
    {
        
        IReadOnlyList<CountryReportDto> expected = [new("GR", "GRC", "Greece", 5), new("US", "USA", "United States", 12)];
        var repositoryMock = new Mock<ICountryReportRepository>();
        repositoryMock.Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new GetCountryReportQueryHandler(repositoryMock.Object);
        
        IReadOnlyList<CountryReportDto> result = await handler.Handle( new GetCountryReportQuery(), CancellationToken.None);
        
        Assert.Equal(expected, result);
        repositoryMock.Verify(repository => repository.GetAllAsync(CancellationToken.None),Times.Once);
    }
}