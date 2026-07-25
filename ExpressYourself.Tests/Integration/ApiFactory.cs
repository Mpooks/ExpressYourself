using ExpressYourself.Application.Interfaces;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using System.Net;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace ExpressYourself.Tests.Integration
{
    public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime 
    {
        private readonly MsSqlContainer _sqlContainer;
        private readonly RedisContainer _redisContainer;
        public FakeIp2cClient FakeIp2cClient { get; }

        public ApiFactory()
        {
            _sqlContainer  = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            _redisContainer = new RedisBuilder("redis:7.4-alpine").Build();
            FakeIp2cClient = new FakeIp2cClient();
        }

        public async Task InitializeAsync()
        {
            await _sqlContainer.StartAsync();
            await _redisContainer.StartAsync();

            using (IServiceScope scope = Services.CreateScope())
            {
                ExpressYourselfDbContext database =
                    scope.ServiceProvider.GetRequiredService<ExpressYourselfDbContext>();
                await database.Database.EnsureCreatedAsync();
            }
        }



        private void FakeServices(IServiceCollection services)
        {
            services.RemoveAll<IIp2cClient>();
            services.AddSingleton<IIp2cClient>(FakeIp2cClient);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:SqlServer", _sqlContainer.GetConnectionString());
            builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
            builder.UseSetting("Cache:Provider", "Redis");
            builder.UseSetting("RefreshJob:Enabled", "false");
            builder.UseSetting("RateLimiting:IpLookup:PermitLimit", "1000000");
            builder.UseSetting("RateLimiting:CountryReport:PermitLimit", "1000000");
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(FakeServices);
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _sqlContainer.DisposeAsync();
            await _redisContainer.DisposeAsync();
            await base.DisposeAsync();
        }

        public async Task ResetStateAsync()
        {
            using (IServiceScope scope = Services.CreateScope())
            {
                ExpressYourselfDbContext database = scope.ServiceProvider.GetRequiredService<ExpressYourselfDbContext>();
                await database.Database.ExecuteSqlRawAsync("DELETE FROM IpAddresses; DELETE FROM Countries;");
            }

            string connection = _redisContainer.GetConnectionString() + ",allowAdmin=true";
            await using (ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(connection))
            {
                EndPoint[] endpoints = redis.GetEndPoints();
                foreach (EndPoint endpoint in endpoints)
                {
                    await redis.GetServer(endpoint).FlushDatabaseAsync();
                }
            }
        }

    }
}
