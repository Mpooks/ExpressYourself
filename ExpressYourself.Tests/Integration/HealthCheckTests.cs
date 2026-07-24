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
    public sealed class HealthCheckTests
    {
        private sealed class HealthyApiFactory : WebApplicationFactory<Program>
        {
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
                        options.UseInMemoryDatabase("HealthCheckTests"));
                });
            }
        }

        private sealed class DownDatabaseApiFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Development");
                builder.UseSetting("Cache:Provider", "Memory");
                builder.UseSetting("RefreshJob:Enabled", "false");
                builder.UseSetting(
                    "ConnectionStrings:SqlServer",
                    "Server=127.0.0.1,1;Database=none;Connect Timeout=1;TrustServerCertificate=true;");
            }
        }

        [Fact]
        public async Task Live_IsHealthy_EvenWhenDatabaseIsDown()
        {
            using DownDatabaseApiFactory factory = new DownDatabaseApiFactory();
            HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Ready_IsHealthy_WhenDatabaseReachable()
        {
            using HealthyApiFactory factory = new HealthyApiFactory();
            HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health/ready");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("database", body);
        }

        [Fact]
        public async Task Ready_IsUnhealthy_AndHidesSecrets_WhenDatabaseIsDown()
        {
            using DownDatabaseApiFactory factory = new DownDatabaseApiFactory();
            HttpClient client = factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync("/health/ready");
            string body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.DoesNotContain("Server=", body);
            Assert.DoesNotContain("127.0.0.1", body);
        }
    }
}