using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Features.IpInformation.Queries;
using ExpressYourself.Application.Strategies;
using Moq;
using System.Timers;

namespace ExpressYourself.Tests.Application.Features.IpInformation;

public sealed class GetIpInformationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ValidInformation_ReturnsInformationFromProvider()
    {
        //Arrange
        var expected = new IpInformationDto("1.2.3.4","GR","GRC","Greece");
        var providerMock = new Mock<IIpInformationProvider>();
        providerMock.Setup(provider => provider.GetIpInformationAsync("1.2.3.4", It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        var handler = new GetIpInformationQueryHandler(providerMock.Object);
        var query = new GetIpInformationQuery("1.2.3.4");
        //Act
        IpInformationDto result = await handler.Handle(query, CancellationToken.None);
        //Assert
        Assert.Equal(expected, result);
        providerMock.Verify(provider => provider.GetIpInformationAsync("1.2.3.4", CancellationToken.None), Times.Once);
    }
}