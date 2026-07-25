using ExpressYourself.Application.Interfaces;
using System.Net;

namespace ExpressYourself.Tests.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class StampedeTests : IntegrationTestBase
    {
        private readonly ApiFactory _apiFactory;

        public StampedeTests(ApiFactory apiFactory) : base(apiFactory)
        {
            _apiFactory = apiFactory;
        }

        [Fact]
        public async Task Lookup_SameIpNTimes_CallsIp2cOnce()
        {
            //Arrange
            const int repeatingRequestCount = 10;
            _apiFactory.FakeIp2cClient.ReturnAfterDelay(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"), TimeSpan.FromMilliseconds(200));
            HttpClient httpClient = _apiFactory.CreateClient();
            List<Task<HttpResponseMessage>> requests = new List<Task<HttpResponseMessage>>();

            //Act
            for (int i = 0; i < repeatingRequestCount; i++)
            {
                requests.Add(httpClient.GetAsync("api/ips/5.5.5.5"));
            }
            HttpResponseMessage[] responseList = await Task.WhenAll(requests);

            //Assert
            foreach (HttpResponseMessage response in responseList)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            Assert.Equal(1, _apiFactory.FakeIp2cClient.CallCounter);
        }
    }
}
