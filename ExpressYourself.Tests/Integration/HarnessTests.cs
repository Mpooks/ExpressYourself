using ExpressYourself.Application.Interfaces;
using System.Net;
using System.Runtime.CompilerServices;

namespace ExpressYourself.Tests.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class HarnessTests : IntegrationTestBase
    {
        private readonly ApiFactory _apiFactory;

        public HarnessTests(ApiFactory apiFactory) : base(apiFactory)
        {
            _apiFactory = apiFactory;
        }

        [Fact]
        public async Task LookupResult_KnownIp_ReturnsOk()
        {
            //Arrange
            _apiFactory.FakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            HttpClient client = _apiFactory.CreateClient();
            
            //Act
            HttpResponseMessage responseMessage = await client.GetAsync("/api/ips/8.8.8.8");

            //Assert
            string body = await responseMessage.Content.ReadAsStringAsync();
            Assert.True(responseMessage.StatusCode == HttpStatusCode.OK, $"STATUS={responseMessage.StatusCode} BODY={body}");
        }
    }
}
