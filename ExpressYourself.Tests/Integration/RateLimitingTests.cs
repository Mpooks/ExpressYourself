using System.Net;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpressYourself.Tests.Integration
{
    [Trait("Category", "Integration")]
    public sealed class RateLimitingTests
    {
        private const int IpLookupLimit = 3;

        private sealed class RateLimitApiFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Cache:Provider", "Memory");
                builder.UseSetting("RefreshJob:Enabled", "false");
                builder.UseSetting("RateLimiting:IpLookup:PermitLimit", IpLookupLimit.ToString());
                builder.UseSetting("RateLimiting:IpLookup:WindowSeconds", "60");

                builder.ConfigureTestServices(services =>
                {
                    List<ServiceDescriptor> efDescriptors = services
                        .Where(descriptor =>
                            descriptor.ServiceType == typeof(DbContextOptions<ExpressYourselfDbContext>) ||
                            (descriptor.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration") ?? false))
                        .ToList();

                    foreach (ServiceDescriptor descriptor in efDescriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ExpressYourselfDbContext>(options =>
                        options.UseInMemoryDatabase("RateLimitingTests"));

                    FakeIp2cClient fake = new FakeIp2cClient();
                    fake.Returns(new Ip2cLookupResult(Ip2cLookupStatus.Success, "GR", "GRC", "Greece"));
                    services.RemoveAll<IIp2cClient>();
                    services.AddSingleton<IIp2cClient>(fake);
                });
            }
        }

        [Fact]
        public async Task IpLookup_WhenLimitExceeded_Returns429()
        {
            using RateLimitApiFactory factory = new RateLimitApiFactory();
            HttpClient client = factory.CreateClient();

            List<HttpStatusCode> statusCodes = new List<HttpStatusCode>();
            for (int i = 0; i < IpLookupLimit + 2; i++)
            {
                HttpResponseMessage response = await client.GetAsync("/api/ips/8.8.8.8");
                statusCodes.Add(response.StatusCode);
            }

            int rejected = statusCodes.Count(code => code == HttpStatusCode.TooManyRequests);
            Assert.Equal(2, rejected);

            Assert.DoesNotContain(HttpStatusCode.TooManyRequests, statusCodes.Take(IpLookupLimit));
        }
    }
}