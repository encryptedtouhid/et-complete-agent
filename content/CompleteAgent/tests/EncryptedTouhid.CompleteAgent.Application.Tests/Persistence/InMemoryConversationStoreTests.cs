using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Conversations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Application.Tests.Persistence;

public sealed class InMemoryConversationStoreTests
{
    [Fact]
    public async Task LoadAsync_EmptyConversation_ReturnsEmpty()
    {
        var store = CreateStore();

        var result = await store.LoadAsync("conv1");

        Assert.Empty(result);
    }

    [Fact]
    public async Task AppendAsync_PersistsAndLoadsInOrder()
    {
        var store = CreateStore();

        await store.AppendAsync("conv1", new ChatMessage(ChatRole.User, "hello"));
        await store.AppendAsync("conv1", new ChatMessage(ChatRole.Assistant, "hi"));

        var result = await store.LoadAsync("conv1");

        Assert.Collection(result,
            m => Assert.Equal("hello", m.Text),
            m => Assert.Equal("hi", m.Text));
    }

    [Fact]
    public async Task AppendAsync_TrimsToMaxMessages()
    {
        var store = CreateStore(maxMessages: 3);

        for (var i = 0; i < 5; i++)
        {
            await store.AppendAsync("c", new ChatMessage(ChatRole.User, $"m{i}"));
        }

        var result = await store.LoadAsync("c");

        Assert.Equal(3, result.Count);
        Assert.Equal("m2", result[0].Text);
        Assert.Equal("m4", result[^1].Text);
    }

    [Fact]
    public async Task ClearAsync_RemovesConversation()
    {
        var store = CreateStore();
        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "hi"));

        await store.ClearAsync("c");

        Assert.Empty(await store.LoadAsync("c"));
    }

    [Fact]
    public async Task ConversationsAreIsolatedById()
    {
        var store = CreateStore();
        await store.AppendAsync("a", new ChatMessage(ChatRole.User, "alpha"));
        await store.AppendAsync("b", new ChatMessage(ChatRole.User, "beta"));

        var a = await store.LoadAsync("a");
        var b = await store.LoadAsync("b");

        Assert.Single(a);
        Assert.Single(b);
        Assert.Equal("alpha", a[0].Text);
        Assert.Equal("beta", b[0].Text);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoadAsync_RejectsNullOrWhitespaceId(string? id)
    {
        var store = CreateStore();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.LoadAsync(id!));
    }

    private static InMemoryConversationStore CreateStore(int maxMessages = 50, int ttlMinutes = 60)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new ConversationOptions
        {
            MaxMessagesPerConversation = maxMessages,
            TtlMinutes = ttlMinutes
        });
        return new InMemoryConversationStore(cache, options);
    }
}
