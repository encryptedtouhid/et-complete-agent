using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests;

[Trait(TestCategories.Name, TestCategories.Integration)]
public sealed class SqlServerConversationStoreTests : IClassFixture<MsSqlFixture>
{
    private readonly MsSqlFixture _fixture;

    public SqlServerConversationStoreTests(MsSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RoundtripsAcrossSqlServer()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("c1", new ChatMessage(ChatRole.User, "hi"));
        await store.AppendAsync("c1", new ChatMessage(ChatRole.Assistant, "hello"));

        var rows = await store.LoadAsync("c1");

        Assert.Equal(2, rows.Count);
        Assert.Equal("hi", rows[0].Text);
        Assert.Equal("hello", rows[1].Text);
    }

    [Fact]
    public async Task TrimsToMaxMessages()
    {
        var store = await NewStoreAsync(new ConversationOptions { MaxMessagesPerConversation = 2, TtlMinutes = 60 });

        await store.AppendAsync("c2", new ChatMessage(ChatRole.User, "a"));
        await store.AppendAsync("c2", new ChatMessage(ChatRole.User, "b"));
        await store.AppendAsync("c2", new ChatMessage(ChatRole.User, "c"));

        var rows = await store.LoadAsync("c2");

        Assert.Equal(2, rows.Count);
        Assert.Equal("b", rows[0].Text);
        Assert.Equal("c", rows[1].Text);
    }

    [Fact]
    public async Task ClearRemovesAllRows()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("c3", new ChatMessage(ChatRole.User, "x"));
        await store.ClearAsync("c3");

        Assert.Empty(await store.LoadAsync("c3"));
    }

    private Task<EfCoreConversationStore> NewStoreAsync(ConversationOptions? options = null)
    {
        var ctxOpts = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlServer(_fixture.ConnectionString)
            .Options;
        return RelationalStoreHarness.BootAsync(ctxOpts, options);
    }
}
