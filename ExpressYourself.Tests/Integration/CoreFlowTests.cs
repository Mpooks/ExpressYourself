
using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Features.CountryReports.Contracts;
using ExpressYourself.Application.Features.IpInformation.Contracts;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Domain.Entities;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ExpressYourself.Tests.Integration
{
    [Collection(IntegrationCollection.Name)]
    [Trait("Category", "Integration")]
    public sealed class CoreFlowTests : IntegrationTestBase
    {
        private readonly ApiFactory _apiFactory;

        public CoreFlowTests(ApiFactory apiFactory) : base(apiFactory)
        {
           _apiFactory = apiFactory;
        }

        [Fact]
        public async Task Refresh_RunsTwice_NoDuplicateRowsIdempotent()
        {
            //Arrange
            await SeedIpWithCountryAsync("15.15.15.15", "GR", "GRC", "Greece");

            _apiFactory.FakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
            HttpClient httpClient = _apiFactory.CreateClient();

            //Act
            HttpResponseMessage first = await httpClient.PostAsync("/api/admin/refresh", null);
            HttpResponseMessage second = await httpClient.PostAsync("/api/admin/refresh", null);

            //Assert
            Assert.True(first.IsSuccessStatusCode);
            Assert.True(second.IsSuccessStatusCode);
            var rows = await CountIpRowsAsync("15.15.15.15");
            Assert.Equal(1, rows);
        }

        [Fact]
        public async Task Report_GroupAddressByCountry_ReturnsGrouped()
        {
            //Arrange
            await SeedIpWithCountryAsync("110.0.0.1", "NZ", "NZL", "New Zealand");
            await SeedIpWithCountryAsync("110.0.0.2", "NZ", "NZL", "New Zealand");
            await SeedIpWithCountryAsync("120.0.0.1", "SE", "SWE", "Sweden");
            HttpClient httpClient = _apiFactory.CreateClient();

            //Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/reports/countries?codes=NZ&codes=SE");

            //Assert
            List<CountryReportDto>? report = await response.Content.ReadFromJsonAsync<List<CountryReportDto>>();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(report);
            Assert.Equal(2, report.Single(c => c.CountryName == "New Zealand").AddressesCount);
            Assert.Equal(1, report.Single(c => c.CountryName == "Sweden").AddressesCount);
        }

        [Fact]
        public async Task Refresh_WhenCountryInvalidates_InvalidateCache()
        {
            //Arrange
            await SeedIpWithCountryAsync("6.6.6.6", "GR", "GRC", "Greece");
            await SeedCacheAsync("6.6.6.6", new IpInformationCacheEntry("Greece", "GR", "GRC"));
            _apiFactory.FakeIp2cClient.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "AL", "ALB", "Albania"));
            HttpClient httpClient = _apiFactory.CreateClient();

            //Act
            HttpResponseMessage refreshed = await httpClient.PostAsync("api/admin/refresh", null);
            HttpResponseMessage lookup = await httpClient.GetAsync("api/ips/6.6.6.6");

            //Assert
            IpInformationDto? dto = await lookup.Content.ReadFromJsonAsync<IpInformationDto>();
            Assert.True(refreshed.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.OK, lookup.StatusCode);
            Assert.NotNull(dto);
            Assert.Equal("AL", dto.TwoLetterCountryCode);
        }

        #region Helper functions

        private async Task SeedIpWithCountryAsync(string address, string twoLetterCode, string threeLetterCode, string countryName)
        {
            using IServiceScope scope = _apiFactory.Services.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;
            ICountryRepository countries = sp.GetRequiredService<ICountryRepository>();
            IIpAddressRepository ips = sp.GetRequiredService<IIpAddressRepository>();
            IUnitOfWork unitOfWork = sp.GetRequiredService<IUnitOfWork>();

            if (await countries.GetByTwoLetterCodeAsync(twoLetterCode, CancellationToken.None) is null)
            {
                countries.Add(new ExpressYourself.Domain.Entities.Country(twoLetterCode, threeLetterCode, countryName));
            }

            IpAddress ipAddress = new IpAddress(address);
            ipAddress.SetCountry(twoLetterCode, DateTimeOffset.UtcNow);
            ips.Add(ipAddress);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        private async Task<int> CountIpRowsAsync(string address)
        {
            using IServiceScope scope = _apiFactory.Services.CreateScope();
            ExpressYourselfDbContext ctx = scope.ServiceProvider.GetRequiredService<ExpressYourselfDbContext>();
            return await ctx.IpAddresses.CountAsync(i => i.Address == address);
        }

        private async Task SeedCacheAsync(string address, IpInformationCacheEntry entry)
        {
            using IServiceScope scope = _apiFactory.Services.CreateScope();
            IIpInformationCache cache = scope.ServiceProvider.GetRequiredService<IIpInformationCache>();
            await cache.SetAsync(address, entry, CancellationToken.None);
        }

        #endregion
    }
}
