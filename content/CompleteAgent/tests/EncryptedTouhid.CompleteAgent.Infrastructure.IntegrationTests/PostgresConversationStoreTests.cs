using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests;

[Trait(TestCategories.Name, TestCategories.Integration)]
public sealed class PostgresConversationStoreTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresConversationStoreTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RoundtripsAcrossPostgres()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("p1", new ChatMessage(ChatRole.User, "hi"));
        await store.AppendAsync("p1", new ChatMessage(ChatRole.Assistant, "hello"));

        var rows = await store.LoadAsync("p1");

        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task TrimsToMaxMessages()
    {
        var store = await NewStoreAsync(new ConversationOptions { MaxMessagesPerConversation = 2, TtlMinutes = 60 });

        await store.AppendAsync("p2", new ChatMessage(ChatRole.User, "a"));
        await store.AppendAsync("p2", new ChatMessage(ChatRole.User, "b"));
        await store.AppendAsync("p2", new ChatMessage(ChatRole.User, "c"));

        Assert.Equal(2, (await store.LoadAsync("p2")).Count);
    }

    [Fact]
    public async Task ClearRemovesAllRows()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("p3", new ChatMessage(ChatRole.User, "x"));
        await store.ClearAsync("p3");

        Assert.Empty(await store.LoadAsync("p3"));
    }

    private Task<EfCoreConversationStore> NewStoreAsync(ConversationOptions? options = null)
    {
        var ctxOpts = new DbContextOptionsBuilder<AgentDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return RelationalStoreHarness.BootAsync(ctxOpts, options);
    }
}
