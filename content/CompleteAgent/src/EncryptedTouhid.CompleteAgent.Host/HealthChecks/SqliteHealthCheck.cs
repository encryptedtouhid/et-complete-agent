using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EncryptedTouhid.CompleteAgent.Host.HealthChecks;

internal sealed class SqliteHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AgentDbContext> _factory;

    public SqliteHealthCheck(IDbContextFactory<AgentDbContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
            var canConnect = await ctx.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("SQLite reachable")
                : HealthCheckResult.Unhealthy("SQLite cannot connect");
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            return HealthCheckResult.Unhealthy("SQLite error", ex);
        }
    }
}
