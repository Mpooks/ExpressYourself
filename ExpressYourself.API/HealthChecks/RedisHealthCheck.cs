using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExpressYourself.API.HealthChecks;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _distributedCache;

    public RedisHealthCheck(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.GetAsync("health:ping", cancellationToken);
            return HealthCheckResult.Healthy("Redis reachable.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Redis unreachable.");
        }
    }
}