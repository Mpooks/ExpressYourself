using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.CountryReports.Queries;
using ExpressYourself.Application.Infrastructure.Persistence;
using Moq;

namespace ExpressYourself.Tests.Application.Features.CountryReports;

public sealed class GetCountryReportQueryHandlerTests
{
    private readonly Mock<ICountryReportRepository> _repo = new();
    private GetCountryReportQueryHandler Handler => new(_repo.Object);

    [Fact]
    public async Task Handle_NoCodes_PassesNullAndReturnsAll()
    {
        IReadOnlyList<CountryReportDto> expected = [new("Greece", 5, DateTimeOffset.UtcNow), new("United States", 12, DateTimeOffset.UtcNow)];
        _repo.Setup(r => r.GetAllAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var result = await Handler.Handle(new GetCountryReportQuery(), CancellationToken.None);
        Assert.Equal(expected, result);
        _repo.Verify(r => r.GetAllAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NormalisesCountryCodes_CodesAreNormalised()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await Handler.Handle(new GetCountryReportQuery([" gr ", "GR", "us"]), CancellationToken.None);
        _repo.Verify(r => r.GetAllAsync(It.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "GR", "US" })),It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCode_ThrowsMessage()
        => await Assert.ThrowsAsync<InvalidCountryCodeException>(() =>
               Handler.Handle(new GetCountryReportQuery(["STRING"]), CancellationToken.None));
}