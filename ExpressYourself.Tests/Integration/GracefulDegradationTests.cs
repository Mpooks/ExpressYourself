using ExpressYourself.Application.Interfaces;
using ExpressYourself.Gateway.Exceptions;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace ExpressYourself.Tests.Integration
{
    [Trait("Category", "Integration")]
    public sealed class GracefulDegradationTests
    {
        private sealed class Ip2cDownApiFactory : WebApplicationFactory<Program>
        {
            private readonly Exception _ip2cFailure;

            public Ip2cDownApiFactory(Exception ip2cFailure)
            {
                _ip2cFailure = ip2cFailure;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Cache:Provider", "Memory");
                builder.UseSetting("RefreshJob:Enabled", "false");

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
                        options.UseInMemoryDatabase("GracefulDegradationTests"));

                    FakeIp2cClient fake = new FakeIp2cClient();
                    fake.Throws(_ip2cFailure);
                    services.RemoveAll<IIp2cClient>();
                    services.AddSingleton<IIp2cClient>(fake);
                });
            }
        }

        [Fact]
        public async Task IpLookup_WhenIp2cUnavailable_Returns503()
        {
            using Ip2cDownApiFactory factory =
                new Ip2cDownApiFactory(new Ip2cUnavailableException("IP2C is unavailable."));
            HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/api/ips/8.8.8.8");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task IpLookup_WhenIp2cReturnsMalformedResponse_Returns502()
        {
            using Ip2cDownApiFactory factory =
                new Ip2cDownApiFactory(new Ip2cResponseFormatException("Upstream returned malformed data."));
            HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/api/ips/8.8.8.8");

            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }
    }
}