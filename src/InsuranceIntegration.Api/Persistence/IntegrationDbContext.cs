using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Persistence;

public sealed class IntegrationDbContext : DbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<KnownSubmissionEntity> KnownSubmissions => Set<KnownSubmissionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnownSubmissionEntity>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.ExternalReference).IsRequired().HasMaxLength(128);
            entity.Property(record => record.InsuredName).IsRequired().HasMaxLength(256);
            entity.Property(record => record.ProductCode).IsRequired().HasMaxLength(64);
            entity.Property(record => record.BrokerCode).HasMaxLength(64);
            entity.HasIndex(record => new { record.ProductCode, record.UnderwritingYear });
        });
    }
}
