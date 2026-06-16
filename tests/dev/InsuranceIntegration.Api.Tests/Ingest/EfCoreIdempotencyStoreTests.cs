using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Responses.Ingest;
using InsuranceIntegration.Api.Services.Ingest;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Tests.Ingest;

public sealed class EfCoreIdempotencyStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public EfCoreIdempotencyStoreTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new IntegrationDbContext(_options);
        context.Database.EnsureCreated();
    }

    [Test]
    public async Task StoreAsync_ThenFindAsync_ReturnsPersistedReceipt()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        var receipt = new IngestReceipt
        {
            EnvelopeId = "env-1",
            Source = "CONTOSO_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "RiskIngestHandler",
            CorrelationId = "corr-1",
            Outcome = new { status = "ok" }
        };

        await store.StoreAsync("CONTOSO_UW", "env-1", receipt);

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        var retrieved = await readStore.FindAsync("CONTOSO_UW", "env-1");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.EnvelopeId, Is.EqualTo("env-1"));
        Assert.That(retrieved.ProcessedBy, Is.EqualTo("RiskIngestHandler"));
    }

    [Test]
    public async Task FindAsync_ReturnsPersistedReceipt()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        await store.StoreAsync("CONTOSO_UW", "env-find", new IngestReceipt
        {
            EnvelopeId = "env-find",
            Source = "CONTOSO_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "RiskIngestHandler"
        });

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        var found = await readStore.FindAsync("CONTOSO_UW", "env-find");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.EnvelopeId, Is.EqualTo("env-find"));
    }

    [Test]
    public async Task FindAsync_ReturnsNullForUnknownKey()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        var found = await store.FindAsync("CONTOSO_UW", "missing");

        Assert.That(found, Is.Null);
    }

    [Test]
    public async Task StoreAsync_FirstWriterWins_OnConcurrentInsertForSameKey()
    {
        using var context1 = new IntegrationDbContext(_options);
        var store1 = new EfCoreIdempotencyStore(context1, TimeProvider.System);

        var first = await store1.StoreAsync("CONTOSO_UW", "env-2", new IngestReceipt
        {
            EnvelopeId = "env-2",
            Source = "CONTOSO_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "First"
        });

        using var context2 = new IntegrationDbContext(_options);
        var store2 = new EfCoreIdempotencyStore(context2, TimeProvider.System);

        var second = await store2.StoreAsync("CONTOSO_UW", "env-2", new IngestReceipt
        {
            EnvelopeId = "env-2",
            Source = "CONTOSO_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "Second"
        });

        // The losing writer receives the canonical (first) receipt instead of overwriting it.
        Assert.That(first.ProcessedBy, Is.EqualTo("First"));
        Assert.That(second.ProcessedBy, Is.EqualTo("First"));

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        var retrieved = await readStore.FindAsync("CONTOSO_UW", "env-2");

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ProcessedBy, Is.EqualTo("First"));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
