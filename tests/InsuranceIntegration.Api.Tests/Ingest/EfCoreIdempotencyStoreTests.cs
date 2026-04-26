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
    public void StoreThenTryGet_ReturnsPersistedReceipt()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        var receipt = new IngestReceipt
        {
            EnvelopeId = "env-1",
            Source = "POLARIS_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "RiskIngestHandler",
            CorrelationId = "corr-1",
            Outcome = new { status = "ok" }
        };

        store.Store("POLARIS_UW", "env-1", receipt);

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        var hit = readStore.TryGet("POLARIS_UW", "env-1", out var retrieved);

        Assert.That(hit, Is.True);
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.EnvelopeId, Is.EqualTo("env-1"));
        Assert.That(retrieved.ProcessedBy, Is.EqualTo("RiskIngestHandler"));
    }

    [Test]
    public void Find_ReturnsPersistedReceipt()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        store.Store("POLARIS_UW", "env-find", new IngestReceipt
        {
            EnvelopeId = "env-find",
            Source = "POLARIS_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "RiskIngestHandler"
        });

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        var found = readStore.Find("POLARIS_UW", "env-find");

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.EnvelopeId, Is.EqualTo("env-find"));
    }

    [Test]
    public void TryGet_ReturnsFalseForUnknownKey()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        var hit = store.TryGet("POLARIS_UW", "missing", out var retrieved);

        Assert.That(hit, Is.False);
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void Find_ReturnsNullForUnknownKey()
    {
        using var context = new IntegrationDbContext(_options);
        var store = new EfCoreIdempotencyStore(context, TimeProvider.System);

        var found = store.Find("POLARIS_UW", "missing");

        Assert.That(found, Is.Null);
    }

    [Test]
    public void Store_OverwritesExistingEntryForSameKey()
    {
        using var context1 = new IntegrationDbContext(_options);
        var store1 = new EfCoreIdempotencyStore(context1, TimeProvider.System);

        store1.Store("POLARIS_UW", "env-2", new IngestReceipt
        {
            EnvelopeId = "env-2",
            Source = "POLARIS_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "First"
        });

        using var context2 = new IntegrationDbContext(_options);
        var store2 = new EfCoreIdempotencyStore(context2, TimeProvider.System);

        store2.Store("POLARIS_UW", "env-2", new IngestReceipt
        {
            EnvelopeId = "env-2",
            Source = "POLARIS_UW",
            MessageType = "RiskSubmission",
            ProcessedBy = "Second"
        });

        using var readContext = new IntegrationDbContext(_options);
        var readStore = new EfCoreIdempotencyStore(readContext, TimeProvider.System);

        readStore.TryGet("POLARIS_UW", "env-2", out var retrieved);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.ProcessedBy, Is.EqualTo("Second"));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
