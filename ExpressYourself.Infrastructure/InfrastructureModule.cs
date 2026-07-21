using Autofac;
using Autofac.Core;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure.Caching.Configuration;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ExpressYourself.Infrastructure
{
    public class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
            builder.RegisterType<CountryRepository>().As<ICountryRepository>().InstancePerLifetimeScope();
            builder.RegisterType<IpAddressRepository>().As<IIpAddressRepository>().InstancePerLifetimeScope();

            builder.RegisterType<Ip2cIpInformationProvider>().Named<IIpInformationProvider>("ip2c").InstancePerLifetimeScope();

            builder.RegisterType<DatabaseIpInformationProvider>().Named<IIpInformationProvider>("database")
                   .WithParameter(ResolvedParameter.ForNamed<IIpInformationProvider>("ip2c"))
                   .InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var inner = c.ResolveNamed<IIpInformationProvider>("database");
                var cache = c.Resolve<IMemoryCache>();
                var options = c.Resolve<IOptions<CacheOptions>>().Value;
                return new CachedIpInformationProvider(inner, cache, TimeSpan.FromMinutes(options.DefaultTtlMinutes));
            })
            .As<IIpInformationProvider>()
            .InstancePerLifetimeScope();
        }
    }
}

