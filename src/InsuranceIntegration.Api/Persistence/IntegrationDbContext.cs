using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Persistence;

public sealed class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<KnownSubmissionEntity> KnownSubmissions => Set<KnownSubmissionEntity>();

    public DbSet<IngestEntryEntity> IngestEntries => Set<IngestEntryEntity>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

    public DbSet<PolicySnapshotEntity> PolicySnapshots => Set<PolicySnapshotEntity>();

    public DbSet<QuoteSnapshotEntity> QuoteSnapshots => Set<QuoteSnapshotEntity>();

    public DbSet<DomainEventEntity> DomainEvents => Set<DomainEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownSubmissionEntity>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever();
            entity.Property(record => record.ExternalReference).IsRequired().HasMaxLength(128);
            entity.Property(record => record.InsuredName).IsRequired().HasMaxLength(256);
            entity.Property(record => record.ProductCode).IsRequired().HasMaxLength(64);
            entity.Property(record => record.BrokerCode).HasMaxLength(64);
            entity.HasIndex(record => new { record.ProductCode, record.UnderwritingYear });
        });

        modelBuilder.Entity<PolicySnapshotEntity>(entity =>
        {
            entity.HasKey(record => record.PolicyReference);
            entity.Property(record => record.PolicyReference).HasMaxLength(128);
            entity.Property(record => record.QuoteReference).HasMaxLength(128);
            entity.Property(record => record.ProductCode).IsRequired().HasMaxLength(64);
            entity.Property(record => record.CurrentPhase).IsRequired().HasMaxLength(64);
            entity.Property(record => record.SnapshotJson).IsRequired();
            entity.HasIndex(record => record.QuoteReference);
            entity.HasIndex(record => new { record.ProductCode, record.UnderwritingYear });
            entity.HasIndex(record => record.LastUpdatedUtc);
        });

        modelBuilder.Entity<QuoteSnapshotEntity>(entity =>
        {
            entity.HasKey(record => record.QuoteReference);
            entity.Property(record => record.QuoteReference).HasMaxLength(128);
            entity.Property(record => record.PolicyReference).HasMaxLength(128);
            entity.Property(record => record.ProductCode).IsRequired().HasMaxLength(64);
            entity.Property(record => record.CurrentPhase).IsRequired().HasMaxLength(64);
            entity.Property(record => record.SnapshotJson).IsRequired();
            entity.HasIndex(record => record.PolicyReference);
            entity.HasIndex(record => new { record.ProductCode, record.UnderwritingYear });
            entity.HasIndex(record => record.LastUpdatedUtc);
        });

        modelBuilder.Entity<IngestEntryEntity>(entity =>
        {
            entity.HasKey(record => new { record.Source, record.EnvelopeId });
            entity.Property(record => record.Source).IsRequired().HasMaxLength(64);
            entity.Property(record => record.EnvelopeId).IsRequired().HasMaxLength(128);
            entity.Property(record => record.MessageType).IsRequired().HasMaxLength(64);
            entity.Property(record => record.ProcessedBy).IsRequired().HasMaxLength(128);
            entity.Property(record => record.CorrelationId).HasMaxLength(64);
            entity.Property(record => record.OutcomeJson).IsRequired();
            entity.HasIndex(record => record.ReceivedAtUtc);
        });

        modelBuilder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.HasKey(record => record.EventId);
            entity.Property(record => record.EventId).ValueGeneratedNever();
            entity.Property(record => record.AggregateType).IsRequired().HasMaxLength(64);
            entity.Property(record => record.EventType).IsRequired().HasMaxLength(128);
            entity.Property(record => record.PayloadJson).IsRequired();
            entity.Property(record => record.LastError).HasMaxLength(1024);
            entity.HasIndex(record => new { record.DispatchedAtUtc, record.OccurredAtUtc });
            entity.HasIndex(record => new { record.AggregateType, record.AggregateId });
        });

        modelBuilder.Entity<DomainEventEntity>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever();
            entity.Property(record => record.EventType).IsRequired().HasMaxLength(64);
            entity.Property(record => record.AggregateKind).IsRequired().HasMaxLength(32);
            entity.Property(record => record.AggregateKey).IsRequired().HasMaxLength(128);
            entity.Property(record => record.Source).IsRequired().HasMaxLength(64);
            entity.Property(record => record.EnvelopeId).HasMaxLength(128);
            entity.Property(record => record.CorrelationId).HasMaxLength(64);
            entity.Property(record => record.PayloadJson).IsRequired();
            entity.HasIndex(record => new { record.AggregateKind, record.AggregateKey, record.OccurredAtUtc });
            entity.HasIndex(record => new { record.EventType, record.OccurredAtUtc });
            entity.HasIndex(record => new { record.Source, record.EnvelopeId });
        });
    }
}
