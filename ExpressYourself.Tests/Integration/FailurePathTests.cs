using ExpressYourself.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ExpressYourself.Tests.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class FailurePathTests
    {
        private readonly ApiFactory _apiFactory;

        public FailurePathTests(ApiFactory apiFactory) 
        {
            _apiFactory = apiFactory;
        }

        [Fact]
        public async Task Lookup_WhenRedIsUnavailable_FallbackToMemory_Returns200Ok()
        {
            //Arrange
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            WebApplicationFactory<Program> withBrokenRedis = _apiFactory.WithWebHostBuilder(BreakRedis);
            HttpClient httpClient = withBrokenRedis.CreateClient();

            //Act
            HttpResponseMessage responseMessage = await httpClient.GetAsync("/api/ips/3.3.3.3");

            //Assert
            Assert.Equal(HttpStatusCode.OK, responseMessage.StatusCode);
        }

        [Fact]
        public async Task Lookup_WhenDatabaseIsDown_ReturnServerError()
        {
            //Arrange
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            WebApplicationFactory<Program> withBrokenDb = _apiFactory.WithWebHostBuilder(BreakDatabase);
            HttpClient client = withBrokenDb.CreateClient();

            //Act
            HttpResponseMessage responseMessage = await client.GetAsync("/api/ips/4.4.4.4");

            //Assert
            Assert.True((int)responseMessage.StatusCode >= 500, $"expected a 5xx, got {(int)responseMessage.StatusCode}");
        }

        private void BreakDatabase(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:SqlServer", "Server=127.0.0.1,1;Database=ExpressYourself;User Id=sa;Password=x;TrustServerCertificate=true;Connect Timeout=3");
        }
        private void BreakRedis(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,abortConnect=false,connectTimeout=500");
        }
    }
}
