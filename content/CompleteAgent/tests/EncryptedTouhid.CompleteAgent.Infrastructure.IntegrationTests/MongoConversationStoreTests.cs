using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Configuration;
using EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Mongo;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests;

[Trait(TestCategories.Name, TestCategories.Integration)]
public sealed class MongoConversationStoreTests : IClassFixture<MongoFixture>, IAsyncLifetime
{
    private const string DatabaseName = "completeagent-it";
    private const string CollectionName = "conversations";

    private readonly MongoFixture _fixture;
    private IMongoClient _client = null!;
    private IMongoDatabase _database = null!;
    private MongoConversationStore _store = null!;

    public MongoConversationStoreTests(MongoFixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _client = new MongoClient(_fixture.ConnectionString);
        _database = _client.GetDatabase(DatabaseName);

        var persistence = Options.Create(new PersistenceOptions
        {
            ConversationStore = ConversationStoreKind.Mongo,
            Mongo = new MongoOptions
            {
                Database = DatabaseName,
                ConversationsCollection = CollectionName,
                AuditCollection = "audit"
            }
        });
        var conversation = Options.Create(new ConversationOptions { MaxMessagesPerConversation = 3, TtlMinutes = 60 });

        _store = new MongoConversationStore(_database, persistence, conversation, TimeProvider.System);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        try { await _client.DropDatabaseAsync(DatabaseName); }
        catch (MongoException) { /* ignore */ }
    }

    [Fact]
    public async Task Roundtrips()
    {
        await _store.AppendAsync("mongo-a", new ChatMessage(ChatRole.User, "hi"));
        await _store.AppendAsync("mongo-a", new ChatMessage(ChatRole.Assistant, "hello"));

        var rows = await _store.LoadAsync("mongo-a");

        Assert.Equal(2, rows.Count);
        Assert.Equal("hi", rows[0].Text);
        Assert.Equal("hello", rows[1].Text);
    }

    [Fact]
    public async Task TrimsToMaxMessages()
    {
        await _store.AppendAsync("mongo-b", new ChatMessage(ChatRole.User, "1"));
        await _store.AppendAsync("mongo-b", new ChatMessage(ChatRole.User, "2"));
        await _store.AppendAsync("mongo-b", new ChatMessage(ChatRole.User, "3"));
        await _store.AppendAsync("mongo-b", new ChatMessage(ChatRole.User, "4"));

        var rows = await _store.LoadAsync("mongo-b");

        Assert.Equal(3, rows.Count);
    }

    [Fact]
    public async Task ClearRemovesAll()
    {
        await _store.AppendAsync("mongo-c", new ChatMessage(ChatRole.User, "x"));
        await _store.ClearAsync("mongo-c");

        Assert.Empty(await _store.LoadAsync("mongo-c"));
    }
}
