using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Gateway.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace ExpressYourself.Tests.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category","Integration")]
    public sealed class IpEndpointIntegrationTests
    {
        private ApiFactory _apiFactory;

        public IpEndpointIntegrationTests(ApiFactory apiFactory)
        {
            _apiFactory = apiFactory;
        }

        [Fact]
        public async Task Get_WhenAddressCached_ReturnsOkFromCacheWithoutExternalCall ()
        {
            const string address = "203.0.113.7";
            await SeedCacheAsync(address, new IpInformationCacheEntry("Greece", "GR", "GRC"));
            _apiFactory.fakeIp2cClient.Throws(new Ip2cUnavailableException("Ip2c is unavailable"));
            var client = _apiFactory.CreateClient();

            var response = await client.GetAsync($"/api/ips/{address}");

            await AssertOkAsync(response, address, "GR");
        }

        [Fact]
        public async Task Get_WhenAddressInDatabase_ReturnsOkFromDatabase()
        {
            const string address = "203.0.113.7";
            await SeedDatabaseSuccessAsync(address, "GR", "GRC", "Greece");
            _apiFactory.fakeIp2cClient.Throws(new Ip2cUnavailableException("Ip2c is unavailable"));
            var client = _apiFactory.CreateClient();

            var response = await client.GetAsync($"/api/ips/{address}");

            await AssertOkAsync(response, address, "GR");
        }

        

        [Fact]
        public async Task Get_WhenNotCachedOrStored_FallsBackToIp2cReturnsOkAndPersists()
        {
            const string address = "203.0.113.7";
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            var client = _apiFactory.CreateClient();

            var response = await client.GetAsync($"/api/ips/{address}");

            await AssertOkAsync(response, address, "GR");
        }

        [Fact]
        public async Task Get_WhenAddressMalformed_Returns400()
        {
            const string address = "1234.345.56.7886";
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            var client = _apiFactory.CreateClient();

            var response = await client.GetAsync($"/api/ips/{address}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_WhenUpstreamReportsInvalid_Returns400()
        {
            const string address = "203.0.113.4";
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Invalid, null, null, null));
            HttpClient client = _apiFactory.CreateClient();

            HttpResponseMessage response = await client.GetAsync($"/api/ips/{address}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_WhenUpstreamReportsUnknown_Returns404()
        {
            const string address = "203.0.113.5";
            _apiFactory.fakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Unknown, null, null, null));
            HttpClient client = _apiFactory.CreateClient();

            HttpResponseMessage response = await client.GetAsync($"/api/ips/{address}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_WhenUpstreamReturnsBadFormat_Returns502()
        {
            const string address = "203.0.113.6";
            _apiFactory.fakeIp2cClient.Throws(new Ip2cResponseFormatException("malformed upstream payload"));
            HttpClient client = _apiFactory.CreateClient();

            HttpResponseMessage response = await client.GetAsync($"/api/ips/{address}");

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        [Fact]
        public async Task Get_WhenUpstreamUnavailable_Returns503()
        {
            const string address = "203.0.113.7";
            _apiFactory.fakeIp2cClient.Throws(new Ip2cUnavailableException("upstream down"));
            HttpClient client = _apiFactory.CreateClient();

            HttpResponseMessage response = await client.GetAsync($"/api/ips/{address}");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }


        private async Task AssertOkAsync(HttpResponseMessage response, string address, string expectedTwoLetter)
        {
            string body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK);

            var dto = await response.Content.ReadFromJsonAsync<IpInformationDto>();

            Assert.NotNull(dto);
            Assert.Equal(dto.IpAddress, address);
            Assert.Equal(dto.TwoLetterCountryCode, expectedTwoLetter);
        }

        private async Task SeedCacheAsync(string address, IpInformationCacheEntry ipInformationCacheEntry)
        {
            using IServiceScope scope = _apiFactory.Services.CreateScope();
            IIpInformationCache cache = scope.ServiceProvider.GetRequiredService<IIpInformationCache>();
            await cache.SetAsync(address, ipInformationCacheEntry, CancellationToken.None);
        }

        private async Task SeedDatabaseSuccessAsync(string address, string twoLetter, string threeLetter, string countryName)
        {
            using IServiceScope scope = _apiFactory.Services.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            var countries = sp.GetRequiredService<ICountryRepository>();
            var ips = sp.GetRequiredService<IIpAddressRepository>();
            var unitsOfWork = sp.GetService<IUnitOfWork>();

            if (await countries.GetByTwoLetterCodeAsync(twoLetter, CancellationToken.None) is null)
            {
                countries.Add(new Country(twoLetter, threeLetter, countryName));
            }

            var ip = new IpAddress(address);
            ip.SetCountry(twoLetter, DateTimeOffset.UtcNow);
            ips.Add(ip);

            await unitsOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }
}
