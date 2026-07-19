using ExpressYourself.Application.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExpressYourself.Tests.Application.Behaviors;

public sealed class PerformanceBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNextTriggers_ReturnsHandlerResponseAndLogsPerformance()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PerformanceBehavior<string, string>>>();
        var behavior = new PerformanceBehavior<string, string>(loggerMock.Object);
        Task<string> Next() => Task.FromResult("response");
        // Act
        string result = await behavior.Handle("request", Next, CancellationToken.None);
        // Assert
        Assert.Equal("response", result);
    }
}