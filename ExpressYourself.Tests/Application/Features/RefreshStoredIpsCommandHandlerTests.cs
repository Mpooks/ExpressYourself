using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Domain.Entities;
using Moq;

namespace ExpressYourself.Tests.Application.Features.IpRefresh;

public sealed class RefreshStoredIpsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WalksReturnedIps_CountsThemAsScanned()
    {
        var repository = new Mock<IIpAddressRepository>();
        repository
            .SetupSequence(r => r.GetBatchAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IpAddress> { new("1.1.1.1"), new("2.2.2.2") })
            .ReturnsAsync(new List<IpAddress>());

        var handler = new RefreshStoredIpsCommandHandler(repository.Object);

        var result = await handler.Handle(new RefreshStoredIpsCommand(), CancellationToken.None);

        Assert.Equal(2, result.Scanned);
        Assert.Equal(0, result.Changed);
        Assert.Equal(0, result.Unchanged);
        Assert.Equal(0, result.Failed);
    }
}