using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;

public sealed class EfCoreConversationStore : IConversationStore
{
    private readonly IDbContextFactory<AgentDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ConversationOptions _options;

    public EfCoreConversationStore(
        IDbContextFactory<AgentDbContext> contextFactory,
        TimeProvider timeProvider,
        IOptions<ConversationOptions> options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public async Task<IReadOnlyList<ChatMessage>> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var cutoff = _timeProvider.GetUtcNow().AddMinutes(-_options.TtlMinutes);

        var rows = await ctx.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.CreatedAt >= cutoff)
            .OrderBy(m => m.Id)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new ChatMessage(MapRole(r.Role), r.Content)).ToArray();
    }

    public async Task AppendAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(message);

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        ctx.ConversationMessages.Add(new ConversationMessageEntity
        {
            ConversationId = conversationId,
            Role = message.Role.Value,
            Content = message.Text ?? string.Empty,
            CreatedAt = _timeProvider.GetUtcNow()
        });
        await ctx.SaveChangesAsync(cancellationToken);

        var overflow = await ctx.ConversationMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Id)
            .Skip(_options.MaxMessagesPerConversation)
            .ToListAsync(cancellationToken);

        if (overflow.Count > 0)
        {
            ctx.ConversationMessages.RemoveRange(overflow);
            await ctx.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);

        await using var ctx = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await ctx.ConversationMessages
            .Where(m => m.ConversationId == conversationId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static ChatRole MapRole(string role) => role switch
    {
        "user" => ChatRole.User,
        "assistant" => ChatRole.Assistant,
        "system" => ChatRole.System,
        "tool" => ChatRole.Tool,
        _ => new ChatRole(role)
    };
}
