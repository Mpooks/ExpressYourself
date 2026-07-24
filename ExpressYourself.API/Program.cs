using Autofac;
using Autofac.Extensions.DependencyInjection;
using ExpressYourself.API.ExceptionHandler;
using ExpressYourself.Application;
using ExpressYourself.Application.Interfaces;
using ExpressYourself.Gateway.Ip2c;
using ExpressYourself.Infrastructure;
using ExpressYourself.Infrastructure.Caching.Configuration;
using ExpressYourself.Infrastructure.Configuration;
using ExpressYourself.Infrastructure.Persistence.Context;
using ExpressYourself.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Headers;
using ExpressYourself.Application.Configuration;
using ExpressYourself.Infrastructure.Scheduling;
using Quartz;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(
new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(container =>
{
    container.RegisterModule(new ApplicationModule());
    container.RegisterModule(new InfrastructureModule());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<ExpressYourselfDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString(ConnectionStringNames.SqlServer)));

builder.Services
    .AddOptions<Ip2cOptions>()
    .Bind(builder.Configuration.GetSection(Ip2cOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<Ip2cOptions>, Ip2cOptionsValidator>();
Ip2cOptions? ip2cOptions = builder.Configuration.GetSection(Ip2cOptions.SectionName).Get<Ip2cOptions>();
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


ip2cHttpClientBuilder.AddResilienceHandler("Ip2cResiliencePipeline", (pipelineBuilder,context) =>
{
    ILogger logger = context.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Ip2cCircuitBreaker");
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
            FailureRatio = 0.9,
            MinimumThroughput = ip2cOptions.CircuitBreakerFailureCount,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(ip2cOptions.CircuitBreakerDurationSeconds),
            OnOpened = arguments =>
            {
                logger.LogWarning("IP2C circuit braker opened for {BreakDurationSeconds} seconds.",arguments.BreakDuration.TotalSeconds);

                return default;
            },
            OnClosed = arguments =>
            {
                logger.LogInformation("IP2C circuit breaker closed.");

                return default;
            }
    });

    pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(ip2cOptions.TimeoutSeconds));
});

builder.Services.AddMemoryCache();
builder.Services.AddOptions<CacheOptions>()
       .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
       .ValidateDataAnnotations()
       .ValidateOnStart();


builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

CacheOptions? cacheOptions = builder.Configuration
        .GetSection(CacheOptions.SectionName)
        .Get<CacheOptions>();

if (cacheOptions is null)
{
    throw new InvalidOperationException("Cache configuration is missing.");
}

if (cacheOptions.UsesRedis)
{
    string? redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    if (string.IsNullOrWhiteSpace(redisConnectionString))
    {
        throw new InvalidOperationException("Redis connection string is required when Redis cache is enabled.");
    }

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "ExpressYourself:";
    });
}

RefreshJobOptions? refreshOptions = builder.Configuration
    .GetSection(RefreshJobOptions.SectionName)
    .Get<RefreshJobOptions>();

if (refreshOptions is null)
{
    throw new InvalidOperationException("RefreshJob configuration is missing.");
}

builder.Services
    .AddOptions<RefreshJobOptions>()
    .Bind(builder.Configuration.GetSection(RefreshJobOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

if (refreshOptions.Enabled)
{
    builder.Services.AddQuartz(quartz =>
    {
        var jobKey = new JobKey("RefreshStoredIps");

        quartz.AddJob<RefreshStoredIpsJob>(job => job.WithIdentity(jobKey));

        quartz.AddTrigger(trigger => trigger
            .ForJob(jobKey)
            .WithIdentity("RefreshStoredIps-trigger")
            .WithCronSchedule(
                refreshOptions.CronExpression,
                cron => cron.WithMisfireHandlingInstructionDoNothing()));
    });

    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });
}

var app = builder.Build();
app.UseMiddleware<CorrelationLoggingMiddleware>();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}