using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EncryptedTouhid.CompleteAgent.Host.HealthChecks;

/// <summary>
/// Provider-agnostic readiness probe for any EF Core relational backend
/// (SQLite, SQL Server, Azure SQL, PostgreSQL, MySQL).
/// </summary>
internal sealed class RelationalDbHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AgentDbContext> _factory;

    public RelationalDbHealthCheck(IDbContextFactory<AgentDbContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
            var provider = ctx.Database.ProviderName ?? "relational";
            var canConnect = await ctx.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy($"{provider} reachable")
                : HealthCheckResult.Unhealthy($"{provider} cannot connect");
        }
        catch (InvalidOperationException ex)
        {
            return HealthCheckResult.Unhealthy("Relational DB configuration error", ex);
        }
        catch (System.Data.Common.DbException ex)
        {
            return HealthCheckResult.Unhealthy("Relational DB error", ex);
        }
    }
}
