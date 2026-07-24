using ExpressYourself.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ExpressYourself.Tests.Api.UnitTests
{
    public  class CorrelationLoggingMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_AddsTraceIdToLoggingScope_AndCallsNext()
        {
            const string traceId = "test-trace-id";

            var logger =
                new Mock<ILogger<CorrelationLoggingMiddleware>>();

            bool nextCalled = false;

            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new CorrelationLoggingMiddleware(
                next,
                logger.Object);

            var context = new DefaultHttpContext
            {
                TraceIdentifier = traceId
            };

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);

            logger.Verify(
            log => log.BeginScope(
            It.Is<Dictionary<string, object>>(scope =>
            scope["TraceId"].ToString() == traceId)),
            Times.Once);
        }
    }
}
