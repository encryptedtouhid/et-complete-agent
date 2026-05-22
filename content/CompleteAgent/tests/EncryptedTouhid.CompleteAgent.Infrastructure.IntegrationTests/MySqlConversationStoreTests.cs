using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests;

[Trait(TestCategories.Name, TestCategories.Integration)]
public sealed class MySqlConversationStoreTests : IClassFixture<MySqlFixture>
{
    private readonly MySqlFixture _fixture;

    public MySqlConversationStoreTests(MySqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RoundtripsAcrossMySql()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("m1", new ChatMessage(ChatRole.User, "hi"));
        await store.AppendAsync("m1", new ChatMessage(ChatRole.Assistant, "hello"));

        Assert.Equal(2, (await store.LoadAsync("m1")).Count);
    }

    [Fact]
    public async Task TrimsToMaxMessages()
    {
        var store = await NewStoreAsync(new ConversationOptions { MaxMessagesPerConversation = 2, TtlMinutes = 60 });

        await store.AppendAsync("m2", new ChatMessage(ChatRole.User, "a"));
        await store.AppendAsync("m2", new ChatMessage(ChatRole.User, "b"));
        await store.AppendAsync("m2", new ChatMessage(ChatRole.User, "c"));

        Assert.Equal(2, (await store.LoadAsync("m2")).Count);
    }

    [Fact]
    public async Task ClearRemovesAllRows()
    {
        var store = await NewStoreAsync();

        await store.AppendAsync("m3", new ChatMessage(ChatRole.User, "x"));
        await store.ClearAsync("m3");

        Assert.Empty(await store.LoadAsync("m3"));
    }

    private Task<EfCoreConversationStore> NewStoreAsync(ConversationOptions? options = null)
    {
        var ctxOpts = new DbContextOptionsBuilder<AgentDbContext>()
            .UseMySQL(_fixture.ConnectionString)
            .Options;
        return RelationalStoreHarness.BootAsync(ctxOpts, options);
    }
}
