using EncryptedTouhid.CompleteAgent.Application.Audit;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Audit;

public sealed class EfCoreAuditLog : IAuditLog
{
    private readonly IDbContextFactory<AgentDbContext> _factory;

    public EfCoreAuditLog(IDbContextFactory<AgentDbContext> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task AppendAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        ctx.AuditEntries.Add(new AuditEntryEntity
        {
            Timestamp = entry.Timestamp,
            SubjectId = entry.SubjectId,
            ConversationId = entry.ConversationId,
            Operation = entry.Operation,
            InputPreview = entry.InputPreview,
            Success = entry.Success,
            TokenCount = entry.TokenCount,
            DurationMs = entry.DurationMs
        });
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
