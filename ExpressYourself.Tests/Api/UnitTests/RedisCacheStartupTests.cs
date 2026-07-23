using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure.Caching;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ExpressYourself.Tests.Api.UnitTests;

[CollectionDefinition("Redis startup", DisableParallelization = true)]
public sealed class RedisStartupCollection
{
}

[Collection("Redis startup")]
public sealed class RedisCacheStartupTests
{
    private const string ProviderVariable = "Cache__Provider";
    private const string TtlVariable = "Cache__DefaultTtlMinutes";
    private const string ConnectionVariable = "ConnectionStrings__Redis";

    [Fact]
    public void Services_RedisProvider_ResolvesFallbackCache()
    {
        string? previousProvider = Environment.GetEnvironmentVariable(ProviderVariable);

        string? previousTtl = Environment.GetEnvironmentVariable(TtlVariable);

        string? previousConnection = Environment.GetEnvironmentVariable(ConnectionVariable);

        try
        {
            Environment.SetEnvironmentVariable(ProviderVariable,"redis");

            Environment.SetEnvironmentVariable(TtlVariable, "60");

            Environment.SetEnvironmentVariable(ConnectionVariable, "localhost:6379,abortConnect=false");

            using var factory = new WebApplicationFactory<Program>();

            using IServiceScope scope = factory.Services.CreateScope();

            IIpInformationCache cache = scope.ServiceProvider
                    .GetRequiredService<IIpInformationCache>();

            Assert.IsType<FallbackIpInformationCache>(cache);
        }
        finally
        {
            Environment.SetEnvironmentVariable( ProviderVariable, previousProvider);

            Environment.SetEnvironmentVariable(TtlVariable, previousTtl);

            Environment.SetEnvironmentVariable(ConnectionVariable, previousConnection);
        }
    }
}