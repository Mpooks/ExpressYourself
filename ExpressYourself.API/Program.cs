using Autofac;
using Autofac.Extensions.DependencyInjection;
using ExpressYourself.Application;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Gateway.Ip2c;
using ExpressYourself.Infrastructure;
using ExpressYourself.Infrastructure.Configuration;
using ExpressYourself.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Headers;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(
    new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule());
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<ExpressYourselfDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString(ConnectionStringNames.SqlServer)));

builder.Services
    .AddOptions<Ip2cOptions>()
    .Bind(builder.Configuration.GetSection(Ip2cOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<Ip2cOptions>,Ip2cOptionsValidator>();
Ip2cOptions? ip2cOptions =builder.Configuration.GetSection(Ip2cOptions.SectionName).Get<Ip2cOptions>();
if (ip2cOptions is null)
{
    throw new InvalidOperationException("Ip2c configuration is missing");
}

IHttpClientBuilder ip2cHttpClientBuilder =
    builder.Services.AddHttpClient<IIp2cClient, Ip2cClient>(
        (serviceProvider, httpClient) =>
        {
            Ip2cOptions options = serviceProvider
                .GetRequiredService<IOptions<Ip2cOptions>>()
                .Value;
            httpClient.BaseAddress =
                new Uri(options.BaseUrl);
            httpClient.Timeout =
                Timeout.InfiniteTimeSpan;
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "text/plain"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "ExpressYourself App");
        });


ip2cHttpClientBuilder.AddResilienceHandler("Ip2cResiliencePipeline",pipelineBuilder =>
    {
        pipelineBuilder.AddRetry(
            new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = ip2cOptions.RetryCount,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });

        pipelineBuilder.AddCircuitBreaker(
            new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput =ip2cOptions.CircuitBreakerFailureCount,
                SamplingDuration =TimeSpan.FromSeconds(30),
                BreakDuration =TimeSpan.FromSeconds(ip2cOptions.CircuitBreakerDurationSeconds)
            });

        pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(ip2cOptions.TimeoutSeconds));
    });


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
