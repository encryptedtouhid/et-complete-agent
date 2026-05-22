using EncryptedTouhid.CompleteAgent.Application.Conversations;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;
using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.IntegrationTests.Fixtures;

internal static class RelationalStoreHarness
{
    public static async Task<EfCoreConversationStore> BootAsync(
        DbContextOptions<AgentDbContext> ctxOptions,
        ConversationOptions? conversationOptions = null,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        await using (var ctx = new AgentDbContext(ctxOptions))
        {
            await ctx.Database.EnsureCreatedAsync(cancellationToken);
        }

        return new EfCoreConversationStore(
            new FixedFactory(ctxOptions),
            timeProvider ?? TimeProvider.System,
            Options.Create(conversationOptions ?? new ConversationOptions()));
    }

    private sealed class FixedFactory : IDbContextFactory<AgentDbContext>
    {
        private readonly DbContextOptions<AgentDbContext> _options;
        public FixedFactory(DbContextOptions<AgentDbContext> options) => _options = options;
        public AgentDbContext CreateDbContext() => new(_options);
    }
}
