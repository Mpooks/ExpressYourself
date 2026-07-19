using ExpressYourself.Application.Behaviors;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressYourself.Tests.Application.Behaviors
{
    public sealed class LoggingBehaviorTests
    {
        [Fact]
        public async Task Handle_ReturnsResponseAndLogsTwice()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<LoggingBehavior<string, string>>>();
            var behavior = new LoggingBehavior<string, string>(loggerMock.Object);
            Task<string> Next() => Task.FromResult("response");
            // Act
            string result = await behavior.Handle("request", Next, CancellationToken.None);
            // Assert
            Assert.Equal("response", result);
            Assert.Equal(2, loggerMock.Invocations.Count);
        }
    }
}
