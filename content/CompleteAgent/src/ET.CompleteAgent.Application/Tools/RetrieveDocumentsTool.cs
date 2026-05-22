using System.ComponentModel;
using System.Text;
using ET.CompleteAgent.Application.Retrieval;
using Microsoft.Extensions.Logging;

namespace ET.CompleteAgent.Application.Tools;

public sealed partial class RetrieveDocumentsTool
{
    private readonly IDocumentRetriever _retriever;
    private readonly ILogger<RetrieveDocumentsTool> _logger;

    public RetrieveDocumentsTool(IDocumentRetriever retriever, ILogger<RetrieveDocumentsTool> logger)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Description("Retrieves up to N relevant documents from the knowledge base by semantic similarity. Use when the user asks about indexed content.")]
    public async Task<string> RetrieveAsync(
        [Description("The user's question or topic.")] string query,
        [Description("Maximum documents to return (1-10).")] int topK = 3,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        topK = Math.Clamp(topK, 1, 10);
        LogRetrieve(query.Length, topK);

        var hits = await _retriever.SearchAsync(query, topK, cancellationToken);
        if (hits.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var hit in hits)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"[{hit.Id} | score={hit.Score:F3}]");
            builder.AppendLine(hit.Content);
            builder.AppendLine();
        }
        return builder.ToString().TrimEnd();
    }

    [LoggerMessage(LogLevel.Debug, "RAG retrieve invoked for {QueryLength}-char query, topK={TopK}")]
    private partial void LogRetrieve(int queryLength, int topK);
}
