using Autofac;
using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ExpressYourself.Tests.Infrastructure.IpInformation;

public sealed class InfrastructureModuleResolutionTests
{
    [Fact]
    public void Build_ResolvesProviderChainAndMemoryCache_WithTtlFromOptions()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new InfrastructureModule());

        builder.RegisterInstance(new Mock<IIpAddressRepository>().Object);
        builder.RegisterInstance(new Mock<ICountryRepository>().Object);
        builder.RegisterInstance(new Mock<IUnitOfWork>().Object);
        builder.RegisterInstance(new Mock<IIp2cClient>().Object);
        builder.RegisterInstance(TimeProvider.System);
        builder.RegisterInstance<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        builder.RegisterInstance(Options.Create(new CacheOptions { DefaultTtlMinutes = 60 }))
               .As<IOptions<CacheOptions>>();
        builder.RegisterInstance(NullLogger<DatabaseIpInformationProvider>.Instance)
               .As<ILogger<DatabaseIpInformationProvider>>();

        builder.RegisterInstance(
        NullLogger<CachedIpInformationProvider>.Instance)
        .As<ILogger<CachedIpInformationProvider>>();
        using var container = builder.Build();

        var provider = container.Resolve<IIpInformationProvider>();
        var cache = container.Resolve<IIpInformationCache>();

        Assert.IsType<CachedIpInformationProvider>(provider);
        Assert.IsType<MemoryIpInformationCache>(cache);
    }
}