using ExpressYourself.API.Controllers;
using ExpressYourself.Application.Features.IpRefresh.Commands;
using ExpressYourself.Application.Features.IpRefresh.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ExpressYourself.Tests.Api.UnitTests
{
    public sealed class AdminControllerTests
    {
        [Fact]
        public async Task RefreshAsync_SendsCommandAndReturnsOkWithResult()
        {
            var expected = new RefreshStoredIpsResult(10, 3, 6, 1);
            var sender = new Mock<ISender>();
            sender.Setup(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expected);
            var controller = new AdminController(sender.Object);

            IActionResult result = await controller.RefreshAsync(CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expected, ok.Value);
            sender.Verify(s => s.Send(It.IsAny<RefreshStoredIpsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}