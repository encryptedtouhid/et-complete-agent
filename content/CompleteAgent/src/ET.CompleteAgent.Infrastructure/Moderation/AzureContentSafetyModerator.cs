using Azure;
using Azure.AI.ContentSafety;
using Azure.Identity;
using ET.CompleteAgent.Application.Moderation;
using ET.CompleteAgent.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ET.CompleteAgent.Infrastructure.Moderation;

public sealed partial class AzureContentSafetyModerator : IContentModerator
{
    private readonly ContentSafetyClient _client;
    private readonly int _maxAllowedSeverity;
    private readonly ILogger<AzureContentSafetyModerator> _logger;

    public AzureContentSafetyModerator(
        IOptions<ModerationOptions> options,
        ILogger<AzureContentSafetyModerator> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;
        var endpoint = opts.AzureEndpoint
            ?? throw new InvalidOperationException("Moderation:AzureEndpoint must be set when Provider = AzureContentSafety.");
        _client = new ContentSafetyClient(new Uri(endpoint), new DefaultAzureCredential());
        _maxAllowedSeverity = opts.MaxAllowedSeverity;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModerationResult> ModerateAsync(string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ModerationResult(IsAllowed: true);
        }

        try
        {
            var response = await _client.AnalyzeTextAsync(content, cancellationToken);
            var max = response.Value.CategoriesAnalysis
                .Where(c => c.Severity.HasValue)
                .Max(c => (int?)c.Severity!.Value) ?? 0;

            if (max > _maxAllowedSeverity)
            {
                LogBlocked(max);
                return new ModerationResult(IsAllowed: false, Reason: $"Severity {max} exceeded threshold {_maxAllowedSeverity}", Severity: max);
            }
            return new ModerationResult(IsAllowed: true, Severity: max);
        }
        catch (RequestFailedException ex)
        {
            LogModerationError(ex.Status, ex.Message);
            return new ModerationResult(IsAllowed: true);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Content moderation blocked input at severity {Severity}")]
    private partial void LogBlocked(int severity);

    [LoggerMessage(LogLevel.Error, "Content moderation API failed: {Status} {Reason}")]
    private partial void LogModerationError(int status, string reason);
}
