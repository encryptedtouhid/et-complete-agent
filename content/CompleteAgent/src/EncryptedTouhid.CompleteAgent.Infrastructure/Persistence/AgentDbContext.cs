using EncryptedTouhid.CompleteAgent.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EncryptedTouhid.CompleteAgent.Infrastructure.Persistence;

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

        // SQLite has no native DateTimeOffset type — store as UTC ticks so EF can
        // translate range comparisons. SQL Server, Postgres, and MySQL keep native types.
        if (Database.IsSqlite())
        {
            var dtoToTicks = new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

            modelBuilder.Entity<ConversationMessageEntity>()
                .Property(x => x.CreatedAt)
                .HasConversion(dtoToTicks);

            modelBuilder.Entity<AuditEntryEntity>()
                .Property(x => x.Timestamp)
                .HasConversion(dtoToTicks);
        }
    }
}
