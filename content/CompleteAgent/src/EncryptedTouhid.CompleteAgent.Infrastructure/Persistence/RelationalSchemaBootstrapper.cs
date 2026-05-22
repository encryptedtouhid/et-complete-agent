using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;

/// <summary>
/// Idempotently creates the schema for relational backends on startup.
/// Safe for SQLite, SQL Server, Azure SQL, PostgreSQL, and MySQL.
/// For production with schema evolution, replace with EF Core migrations.
/// </summary>
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by IServiceCollection.AddHostedService<T>().")]
internal sealed partial class RelationalSchemaBootstrapper : IHostedService
{
    private readonly IDbContextFactory<AgentDbContext> _factory;
    private readonly ILogger<RelationalSchemaBootstrapper> _logger;

    public RelationalSchemaBootstrapper(
        IDbContextFactory<AgentDbContext> factory,
        ILogger<RelationalSchemaBootstrapper> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        var created = await ctx.Database.EnsureCreatedAsync(cancellationToken);
        LogBootstrapped(ctx.Database.ProviderName ?? "unknown", created);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(LogLevel.Information, "Relational schema bootstrap — provider: {Provider}, created: {Created}")]
    private partial void LogBootstrapped(string provider, bool created);
}
