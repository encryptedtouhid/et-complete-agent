using ET.CompleteAgent.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ET.CompleteAgent.Infrastructure.Persistence;

public sealed class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options) { }

    public DbSet<ConversationMessageEntity> ConversationMessages => Set<ConversationMessageEntity>();

    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<ConversationMessageEntity>(b =>
        {
            b.ToTable("ConversationMessages");
            b.HasIndex(x => new { x.ConversationId, x.Id });
            b.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<AuditEntryEntity>(b =>
        {
            b.ToTable("AuditEntries");
            b.HasIndex(x => x.Timestamp);
            b.HasIndex(x => new { x.SubjectId, x.Timestamp });
        });
    }
}
