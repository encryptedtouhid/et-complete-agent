using EncryptedTouhid.CompleteAgent.Application.Conversations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Conversations;

public sealed class InMemoryConversationStore : IConversationStore
{
    private readonly IMemoryCache _cache;
    private readonly ConversationOptions _options;

    public InMemoryConversationStore(IMemoryCache cache, IOptions<ConversationOptions> options)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public Task<IReadOnlyList<ChatMessage>> LoadAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        var messages = _cache.Get<List<ChatMessage>>(Key(conversationId)) ?? [];
        return Task.FromResult<IReadOnlyList<ChatMessage>>(messages.ToArray());
    }

    public Task AppendAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentNullException.ThrowIfNull(message);

        var key = Key(conversationId);
        var existing = _cache.Get<List<ChatMessage>>(key) ?? [];
        existing.Add(message);

        if (existing.Count > _options.MaxMessagesPerConversation)
        {
            existing.RemoveRange(0, existing.Count - _options.MaxMessagesPerConversation);
        }

        _cache.Set(key, existing, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(_options.TtlMinutes)
        });

        return Task.CompletedTask;
    }

    public Task ClearAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        _cache.Remove(Key(conversationId));
        return Task.CompletedTask;
    }

    private static string Key(string conversationId) => $"conv:{conversationId}";
}
