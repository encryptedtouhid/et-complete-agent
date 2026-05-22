using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests;

[Trait(TestCategories.Name, TestCategories.Integration)]
public sealed class CosmosConversationStoreTests : IClassFixture<CosmosFixture>, IAsyncLifetime
{
    private const string DatabaseName = "completeagent-it";
    private const string ContainerName = "conversations";

    private readonly CosmosFixture _fixture;
    private CosmosClient _client = null!;
    private CosmosConversationStore _store = null!;

    public CosmosConversationStoreTests(CosmosFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _client = _fixture.CreateClient();
        var db = await _client.CreateDatabaseIfNotExistsAsync(DatabaseName);
        await db.Database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(ContainerName, "/conversationId") { DefaultTimeToLive = 3600 });

        var persistence = Options.Create(new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Cosmos,
            Cosmos = new CosmosOptions
            {
                Database = DatabaseName,
                ConversationsContainer = ContainerName,
                AuditContainer = "audit"
            }
        });
        var conversation = Options.Create(new ConversationOptions { MaxMessagesPerConversation = 3, TtlMinutes = 60 });

        _store = new CosmosConversationStore(_client, persistence, conversation, TimeProvider.System);
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _client.GetDatabase(DatabaseName).DeleteAsync();
        }
        catch (CosmosException)
        {
            // ignore — emulator teardown
        }
        _client.Dispose();
    }

    [Fact]
    public async Task Roundtrips()
    {
        await _store.AppendAsync("cos-a", new ChatMessage(ChatRole.User, "hi"));
        await _store.AppendAsync("cos-a", new ChatMessage(ChatRole.Assistant, "hello"));

        var rows = await _store.LoadAsync("cos-a");

        Assert.Equal(2, rows.Count);
        Assert.Equal("hi", rows[0].Text);
        Assert.Equal("hello", rows[1].Text);
    }

    [Fact]
    public async Task TrimsToMaxMessages()
    {
        await _store.AppendAsync("cos-b", new ChatMessage(ChatRole.User, "1"));
        await _store.AppendAsync("cos-b", new ChatMessage(ChatRole.User, "2"));
        await _store.AppendAsync("cos-b", new ChatMessage(ChatRole.User, "3"));
        await _store.AppendAsync("cos-b", new ChatMessage(ChatRole.User, "4"));

        var rows = await _store.LoadAsync("cos-b");

        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task ClearRemovesAll()
    {
        await _store.AppendAsync("cos-c", new ChatMessage(ChatRole.User, "x"));
        await _store.ClearAsync("cos-c");

        Assert.Empty(await _store.LoadAsync("cos-c"));
    }
}
