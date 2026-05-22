using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Host.HealthChecks;

internal sealed class AgentOptionsHealthCheck : IHealthCheck
{
    private readonly IOptionsMonitor<AgentOptions> _options;

    public AgentOptionsHealthCheck(IOptionsMonitor<AgentOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var opts = _options.CurrentValue;

        var hasProviderConfig = opts.Provider switch
        {
            AgentProvider.AzureOpenAI => !string.IsNullOrWhiteSpace(opts.AzureOpenAI.Endpoint),
            AgentProvider.OpenAI => !string.IsNullOrWhiteSpace(opts.OpenAI.ApiKey),
            _ => false
        };

        return Task.FromResult(hasProviderConfig
            ? HealthCheckResult.Healthy($"Provider {opts.Provider}, model {opts.Model}")
            : HealthCheckResult.Unhealthy($"Provider {opts.Provider} is missing required configuration"));
    }
}
