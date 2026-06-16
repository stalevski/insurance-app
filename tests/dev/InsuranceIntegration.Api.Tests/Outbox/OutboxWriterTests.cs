using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Correlation;
using InsuranceIntegration.Api.Services.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class OutboxWriterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<IntegrationDbContext> _options;

    public OutboxWriterTests()
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
    public void Enqueue_AddsOutboxMessageWithCorrelationAndPayload()
    {
        using var context = new IntegrationDbContext(_options);
        var correlation = new CorrelationContext();
        var expectedCorrelation = Guid.CreateVersion7();
        correlation.Set(expectedCorrelation);

        var writer = new OutboxWriter(context, correlation, TimeProvider.System);

        var aggregateId = Guid.CreateVersion7();
        writer.Enqueue("Submission", aggregateId, "SubmissionRegistered", new { externalReference = "EXT-1" });

        context.SaveChanges();

        using var readContext = new IntegrationDbContext(_options);
        var stored = readContext.OutboxMessages.Single();

        Assert.That(stored.AggregateType, Is.EqualTo("Submission"));
        Assert.That(stored.AggregateId, Is.EqualTo(aggregateId));
        Assert.That(stored.EventType, Is.EqualTo("SubmissionRegistered"));
        Assert.That(stored.CorrelationId, Is.EqualTo(expectedCorrelation));
        Assert.That(stored.DispatchedAtUtc, Is.Null);
        Assert.That(stored.DispatchAttempts, Is.EqualTo(0));
        Assert.That(stored.PayloadJson, Does.Contain("EXT-1"));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
