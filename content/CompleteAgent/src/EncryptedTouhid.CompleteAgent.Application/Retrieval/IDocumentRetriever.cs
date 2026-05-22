namespace EncryptedTouhid.CompleteAgent.Application.Retrieval;

public sealed record RetrievedDocument(string Id, string Content, double Score);

public interface IDocumentRetriever
{
    Task IndexAsync(string id, string content, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievedDocument>> SearchAsync(string query, int topK = 3, CancellationToken cancellationToken = default);
}
