using System.Reflection;
using EncryptedTouhid.CompleteAgent.Host.Models;

namespace EncryptedTouhid.CompleteAgent.Host.Endpoints;

internal static class VersionEndpoint
{
    public static IEndpointRouteBuilder MapVersionEndpoint(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/version", Get).AllowAnonymous();
        return app;
    }

    private static VersionInfo Get()
    {
        var assembly = typeof(VersionEndpoint).Assembly;
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        var version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        var (sha, isDirty) = SplitCommit(informational);

        return new VersionInfo(
            version,
            informational,
            sha,
            isDirty,
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");
    }

    private static (string? Sha, bool? IsDirty) SplitCommit(string informational)
    {
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return (null, null);
        }
        var meta = informational[(plus + 1)..];
        var isDirty = meta.EndsWith(".dirty", StringComparison.Ordinal);
        var sha = isDirty ? meta[..^".dirty".Length] : meta;
        return (sha, isDirty);
    }
}
