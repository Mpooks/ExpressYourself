using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using ExpressYourself.Infrastructure.Scheduling;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Xunit;

namespace ExpressYourself.Tests.Infrastructure.Scheduling;

public sealed class RefreshStoredIpsJobTests
{
    private static Mock<IJobExecutionContext> JobContext()
    {
        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Execute_SendsRefreshCommandOnce()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new RefreshStoredIpsResult(5, 2, 3, 0));

        var job = new RefreshStoredIpsJob(sender.Object, NullLogger<RefreshStoredIpsJob>.Instance);

        await job.Execute(JobContext().Object);

        sender.Verify(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_WhenCommandThrows_RethrowsSoQuartzCanRecordFailure()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("boom"));

        var job = new RefreshStoredIpsJob(sender.Object, NullLogger<RefreshStoredIpsJob>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(JobContext().Object));
    }
}
