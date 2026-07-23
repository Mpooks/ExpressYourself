using ExpressYourself.API.Controllers;
using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ExpressYourself.Tests.Api.UnitTests
{
    public sealed class AdminControllerTests
    {
        private static Mock<IHostEnvironment> Environment(string name)
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(e => e.EnvironmentName).Returns(name);
            return environment;
        }

        [Fact]
        public async Task RefreshAsync_InDevelopment_SendsCommandAndReturnsOkWithResult()
        {
            var expected = new RefreshStoredIpsResult(10, 3, 6, 1);
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expected);
            var controller = new AdminController(sender.Object, Environment("Development").Object);

            IActionResult result = await controller.RefreshAsync(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
            sender.Verify(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_NotInDevelopment_ReturnsNotFoundAndDoesNotSend()
        {
            var sender = new Mock<ISender>();
            var controller = new AdminController(sender.Object, Environment("Production").Object);

            IActionResult result = await controller.RefreshAsync(CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
            sender.Verify(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}