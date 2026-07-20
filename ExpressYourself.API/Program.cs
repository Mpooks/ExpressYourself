using Autofac;
using Autofac.Extensions.DependencyInjection;
using ExpressYourself.Application;
using ExpressYourself.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ExpressYourself.Infrastructure.Configuration;
using ExpressYourself.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule());
});
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDbContext<ExpressYourselfDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(ConnectionStringNames.SqlServer)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
