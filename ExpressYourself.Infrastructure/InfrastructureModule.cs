using Autofac;
using Autofac.Core;
using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.Caching.Configuration;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ExpressYourself.Infrastructure.Persistence;
using ExpressYourself.Infrastructure.Persistence.Abstraction;

namespace ExpressYourself.Infrastructure
{
    public class InfrastructureModule : Module
    {
        private const string MemoryCacheName = "memory";
        private const string RedisCacheName = "redis";

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
            builder.RegisterType<CountryRepository>().As<ICountryRepository>().InstancePerLifetimeScope();
            builder.RegisterType<IpAddressRepository>().As<IIpAddressRepository>().InstancePerLifetimeScope();
            builder.RegisterType<MemoryIpInformationCache>().Named<IIpInformationCache>(MemoryCacheName).SingleInstance();
            builder.RegisterType<RedisIpInformationCache>().Named<IIpInformationCache>(RedisCacheName).SingleInstance();

            builder.Register(context =>
            {
                CacheOptions options = context.Resolve<IOptions<CacheOptions>>().Value;

                if (options.UsesMemory)
                {
                    return context.ResolveNamed<IIpInformationCache>(MemoryCacheName);
                }

                if (options.UsesRedis)
                {
                    return CreateRedisCache(context);
                }

                throw new InvalidOperationException($"Unsupported cache provider '{options.Provider}'.");
            })
            .As<IIpInformationCache>()
            .SingleInstance();

            builder.RegisterType<Ip2cIpInformationProvider>()
                   .Named<IIpInformationProvider>("ip2c")
                   .InstancePerLifetimeScope();

            builder.RegisterType<DatabaseIpInformationProvider>()
                   .Named<IIpInformationProvider>("database")
                   .WithParameter(ResolvedParameter.ForNamed<IIpInformationProvider>("ip2c"))
                   .InstancePerLifetimeScope();

            builder.Register(context =>
            {
                var inner = context.ResolveNamed<IIpInformationProvider>("database");

                var cache = context.Resolve<IIpInformationCache>();

                return new CachedIpInformationProvider(inner, cache);
            })
            .As<IIpInformationProvider>()
            .InstancePerLifetimeScope();
            
            builder.RegisterType<SqlConnectionFactory>().As<IDbConnectionFactory>().SingleInstance();
            builder.RegisterType<CountryReportRepository>().As<ICountryReportRepository>().InstancePerLifetimeScope();
        }

        private static IIpInformationCache CreateRedisCache(
            IComponentContext context)
        {
            return new FallbackIpInformationCache(
                context.ResolveNamed<IIpInformationCache>(RedisCacheName),
                context.ResolveNamed<IIpInformationCache>(MemoryCacheName),
                context.Resolve<ILogger<FallbackIpInformationCache>>());
        }
    }
}