using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace EncryptedTouhid.CompleteAgent.Application.Tools;

public sealed partial class SearchKnowledgeBaseTool
{
    private static readonly IReadOnlyDictionary<string, string> SampleIndex =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["agent framework"] = "Microsoft Agent Framework is a .NET and Python framework for building AI agents and multi-agent workflows.",
            ["clean architecture"] = "Layered design with Domain at the centre. Outer layers depend inward only.",
            ["prompt injection"] = "An attack where adversarial text in user input attempts to override system instructions."
        };

    private readonly ILogger<SearchKnowledgeBaseTool> _logger;

    public SearchKnowledgeBaseTool(ILogger<SearchKnowledgeBaseTool> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [LoggerMessage(LogLevel.Debug, "Knowledge base search invoked for query of length {Length}")]
    private partial void LogSearch(int length);

    [Description("Searches the internal knowledge base. Pass a short query and receive a single text snippet, or an empty string if nothing matches.")]
    public string Search(
        [Description("A short query, 1-10 words.")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        LogSearch(query.Length);

        return SampleIndex
            .Where(kvp => query.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .FirstOrDefault() ?? string.Empty;
    }
}
