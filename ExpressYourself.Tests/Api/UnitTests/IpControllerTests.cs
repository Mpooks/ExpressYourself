using ExpressYourself.API.Controllers;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Features.IpInformation.Queries;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Moq;


namespace ExpressYourself.Tests.Api.UnitTests
{
    public class IpControllerTests
    {
        [Fact]
        public async Task GetAsync_WhenSenderReturns_ReturnsOkMessageAndIpInformation() 
        {
            //Arrange
            var expected = new IpInformationDto("2.2.2.2", "SE", "SWE", "Sweden");
            var mockSender = new Mock<ISender>();
            mockSender.Setup(sd => sd.Send(It.Is<GetIpInformationQuery>(qy => qy.IpAddress == "2.2.2.2"), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var controller = new IpController(mockSender.Object);
            //Act
            ActionResult<IpInformationDto> result = await controller.GetAsync("2.2.2.2", CancellationToken.None);
            //Assert
            var okMessage = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(expected, okMessage.Value);
            mockSender.Verify(sd => sd.Send(It.Is<GetIpInformationQuery>(qy => qy.IpAddress == "2.2.2.2"), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
