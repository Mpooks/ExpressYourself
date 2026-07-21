using Autofac;
using Autofac.Core;
using ExpressYourself.Application.Caching;
using ExpressYourself.Application.Infrastructure.Persistence;
using ExpressYourself.Application.Strategies;
using ExpressYourself.Infrastructure.Caching;
using ExpressYourself.Infrastructure.IpInformation.Providers;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.Infrastructure.Persistence.Repositories;

namespace ExpressYourself.Infrastructure
{
    public class InfrastructureModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();
            builder.RegisterType<CountryRepository>().As<ICountryRepository>().InstancePerLifetimeScope();
            builder.RegisterType<IpAddressRepository>().As<IIpAddressRepository>().InstancePerLifetimeScope();

            builder.RegisterType<MemoryIpInformationCache>()
                   .As<IIpInformationCache>()
                   .SingleInstance();

            builder.RegisterType<Ip2cIpInformationProvider>()
                   .Named<IIpInformationProvider>("ip2c")
                   .InstancePerLifetimeScope();

            builder.RegisterType<DatabaseIpInformationProvider>()
                   .Named<IIpInformationProvider>("database")
                   .WithParameter(ResolvedParameter.ForNamed<IIpInformationProvider>("ip2c"))
                   .InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var inner = c.ResolveNamed<IIpInformationProvider>("database");
                var cache = c.Resolve<IIpInformationCache>();

                return new CachedIpInformationProvider(inner, cache);
            })
            .As<IIpInformationProvider>()
            .InstancePerLifetimeScope();
        }
    }
}