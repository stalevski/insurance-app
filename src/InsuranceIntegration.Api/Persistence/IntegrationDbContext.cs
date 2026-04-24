using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Persistence;

public sealed class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<KnownSubmissionEntity> KnownSubmissions => Set<KnownSubmissionEntity>();

    public DbSet<InboxMessageEntity> InboxMessages => Set<InboxMessageEntity>();

    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();

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

        modelBuilder.Entity<InboxMessageEntity>(entity =>
        {
            entity.HasKey(record => new { record.Source, record.EnvelopeId });
            entity.Property(record => record.Source).IsRequired().HasMaxLength(64);
            entity.Property(record => record.EnvelopeId).IsRequired().HasMaxLength(128);
            entity.Property(record => record.Type).IsRequired().HasMaxLength(64);
            entity.Property(record => record.HandlerName).IsRequired().HasMaxLength(128);
            entity.Property(record => record.CorrelationId).HasMaxLength(64);
            entity.Property(record => record.ResultJson).IsRequired();
            entity.HasIndex(record => record.ProcessedAtUtc);
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
    }
}
