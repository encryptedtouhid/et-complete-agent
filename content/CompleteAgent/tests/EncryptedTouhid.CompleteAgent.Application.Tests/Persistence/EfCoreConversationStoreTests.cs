using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace EncryptedTouhid.CompleteAgent.Application.Tests.Persistence;

/// <summary>
/// Contract tests for the EF Core conversation store. SQLite in-memory exercises the same
/// LINQ + ExecuteDeleteAsync code path used by SQL Server, PostgreSQL, and MySQL in prod.
/// </summary>
public sealed class EfCoreConversationStoreTests : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private DbContextOptions<AgentDbContext>? _ctxOptions;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();
        _ctxOptions = new DbContextOptionsBuilder<AgentDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var ctx = new AgentDbContext(_ctxOptions);
        await ctx.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AppendThenLoad_PreservesOrder()
    {
        var store = NewStore(out _);

        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "first"));
        await store.AppendAsync("c", new ChatMessage(ChatRole.Assistant, "second"));

        var rows = await store.LoadAsync("c");

        Assert.Collection(rows,
            m => Assert.Equal("first", m.Text),
            m => Assert.Equal("second", m.Text));
    }

    [Fact]
    public async Task Trim_KeepsOnlyMaxMessages()
    {
        var store = NewStore(out _, maxMessages: 2);

        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "a"));
        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "b"));
        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "c"));

        var rows = await store.LoadAsync("c");

        Assert.Equal(2, rows.Count);
        Assert.Equal("b", rows[0].Text);
        Assert.Equal("c", rows[1].Text);
    }

    [Fact]
    public async Task LoadAsync_FiltersOutExpiredMessagesByTtl()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var store = NewStore(out _, ttlMinutes: 10, time: time);

        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "stale"));
        time.Advance(TimeSpan.FromMinutes(30));
        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "fresh"));

        var rows = await store.LoadAsync("c");

        Assert.Single(rows);
        Assert.Equal("fresh", rows[0].Text);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllRowsForConversation()
    {
        var store = NewStore(out _);

        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "x"));
        await store.AppendAsync("c", new ChatMessage(ChatRole.User, "y"));
        await store.AppendAsync("other", new ChatMessage(ChatRole.User, "z"));

        await store.ClearAsync("c");

        Assert.Empty(await store.LoadAsync("c"));
        Assert.Single(await store.LoadAsync("other"));
    }

    [Fact]
    public async Task ConversationsAreIsolatedById()
    {
        var store = NewStore(out _);

        await store.AppendAsync("a", new ChatMessage(ChatRole.User, "alpha"));
        await store.AppendAsync("b", new ChatMessage(ChatRole.User, "beta"));

        var a = await store.LoadAsync("a");
        var b = await store.LoadAsync("b");

        Assert.Single(a);
        Assert.Single(b);
        Assert.Equal("alpha", a[0].Text);
        Assert.Equal("beta", b[0].Text);
    }

    [Fact]
    public async Task RolesRoundTrip()
    {
        var store = NewStore(out _);

        await store.AppendAsync("c", new ChatMessage(ChatRole.System, "sys"));
        await store.AppendAsync("c", new ChatMessage(ChatRole.Tool, "tool"));

        var rows = await store.LoadAsync("c");

        Assert.Equal(ChatRole.System, rows[0].Role);
        Assert.Equal(ChatRole.Tool, rows[1].Role);
    }

    private EfCoreConversationStore NewStore(
        out FixedDbContextFactory factory,
        int maxMessages = 50,
        int ttlMinutes = 60,
        TimeProvider? time = null)
    {
        factory = new FixedDbContextFactory(_ctxOptions!);
        var options = Options.Create(new ConversationOptions
        {
            MaxMessagesPerConversation = maxMessages,
            TtlMinutes = ttlMinutes
        });
        return new EfCoreConversationStore(factory, time ?? TimeProvider.System, options);
    }

    private sealed class FixedDbContextFactory : IDbContextFactory<AgentDbContext>
    {
        private readonly DbContextOptions<AgentDbContext> _options;
        public FixedDbContextFactory(DbContextOptions<AgentDbContext> options) => _options = options;
        public AgentDbContext CreateDbContext() => new(_options);
    }
}
