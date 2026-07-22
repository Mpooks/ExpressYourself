using Autofac;
using ExpressYourself.Application.Caching;
using ExpressYourself.Infrastructure;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.Caching;

public sealed class CacheProviderResolutionTests
{
    [Fact]
    public void Resolve_MemoryProvider_ReturnsMemoryCache()
    {
        using IContainer container = BuildContainer("Memory");

        IIpInformationCache cache = container.Resolve<IIpInformationCache>();

        Assert.IsType<MemoryIpInformationCache>(cache);
    }

    [Fact]
    public void Resolve_RedisProvider_ReturnsFallbackCache()
    {
        using IContainer container = BuildContainer("Redis");

        IIpInformationCache cache = container.Resolve<IIpInformationCache>();

        Assert.IsType<FallbackIpInformationCache>(cache);
    }

    private static IContainer BuildContainer(string provider)
    {
        var builder = new ContainerBuilder();

        builder.RegisterModule(new InfrastructureModule());

        builder.RegisterInstance(
                Options.Create(
                    new CacheOptions
                    {
                        Provider = provider,
                        DefaultTtlMinutes = 60
                    }))
            .As<IOptions<CacheOptions>>();

        builder.RegisterInstance<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));

        builder.RegisterInstance(new Mock<IDistributedCache>().Object)
            .As<IDistributedCache>();

        builder.RegisterInstance(NullLogger<RedisIpInformationCache>.Instance)
            .As<ILogger<RedisIpInformationCache>>();

        builder.RegisterInstance(NullLogger<FallbackIpInformationCache>.Instance)
            .As<ILogger<FallbackIpInformationCache>>();

        return builder.Build();
    }
}