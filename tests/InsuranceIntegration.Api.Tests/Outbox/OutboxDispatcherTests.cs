using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class OutboxDispatcherTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly RecordingPublisher _publisher = new();

    public OutboxDispatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<IntegrationDbContext>(options => options.UseSqlite(_connection));
        services.AddSingleton<IOutboxPublisher>(_publisher);
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        context.Database.EnsureCreated();
    }

    [SetUp]
    public void ResetDatabase()
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        context.OutboxMessages.RemoveRange(context.OutboxMessages);
        context.SaveChanges();
        _publisher.Published.Clear();
        _publisher.FailWith = null;
    }

    [Test]
    public async Task DispatchBatchAsync_MarksPendingMessagesAsDispatched()
    {
        using (var seedScope = _provider.CreateScope())
        {
            var context = seedScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
            context.OutboxMessages.Add(new OutboxMessageEntity
            {
                EventId = Guid.CreateVersion7(),
                AggregateType = "Submission",
                AggregateId = Guid.CreateVersion7(),
                EventType = "SubmissionRegistered",
                PayloadJson = "{}",
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-10)
            });
            context.OutboxMessages.Add(new OutboxMessageEntity
            {
                EventId = Guid.CreateVersion7(),
                AggregateType = "Claim",
                AggregateId = Guid.CreateVersion7(),
                EventType = "ClaimAcknowledged",
                PayloadJson = "{}",
                OccurredAtUtc = DateTime.UtcNow.AddSeconds(-5)
            });
            context.SaveChanges();
        }

        var dispatcher = new OutboxDispatcher(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxDispatcher>.Instance,
            TimeProvider.System);

        var dispatched = await dispatcher.DispatchBatchAsync(CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(2));
        Assert.That(_publisher.Published, Has.Count.EqualTo(2), "every dispatched message must be published");

        using var verifyScope = _provider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var remaining = verifyContext.OutboxMessages.Count(message => message.DispatchedAtUtc == null);
        Assert.That(remaining, Is.EqualTo(0));

        var attempts = verifyContext.OutboxMessages.Select(message => message.DispatchAttempts).ToArray();
        Assert.That(attempts, Is.EqualTo(new[] { 1, 1 }));
    }

    [Test]
    public async Task DispatchBatchAsync_ReturnsZeroWhenNothingPending()
    {
        var dispatcher = new OutboxDispatcher(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxDispatcher>.Instance,
            TimeProvider.System);

        var dispatched = await dispatcher.DispatchBatchAsync(CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(0));
    }

    [Test]
    public async Task DispatchBatchAsync_PublishFails_LeavesMessagePendingWithError()
    {
        SeedMessage("PolicyBound");
        _publisher.FailWith = new InvalidOperationException("broker down");

        var dispatcher = BuildDispatcher();
        var dispatched = await dispatcher.DispatchBatchAsync(CancellationToken.None);

        Assert.That(dispatched, Is.Zero);

        using var verifyScope = _provider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var message = verifyContext.OutboxMessages.Single();
        Assert.That(message.DispatchedAtUtc, Is.Null, "failed publish must not mark the message dispatched");
        Assert.That(message.DispatchAttempts, Is.EqualTo(1));
        Assert.That(message.LastError, Is.EqualTo("broker down"));
    }

    [Test]
    public async Task DispatchBatchAsync_RetriesFailedMessage_UntilAttemptCapThenSkips()
    {
        SeedMessage("PolicyBound");
        _publisher.FailWith = new InvalidOperationException("still down");

        var dispatcher = BuildDispatcher();
        for (var attempt = 0; attempt < OutboxDispatcher.MaxDispatchAttempts; attempt++)
        {
            await dispatcher.DispatchBatchAsync(CancellationToken.None);
        }

        Assert.That(_publisher.Published, Has.Count.EqualTo(OutboxDispatcher.MaxDispatchAttempts));

        // Poisoned: the capped message must no longer be picked up.
        var dispatched = await dispatcher.DispatchBatchAsync(CancellationToken.None);

        Assert.That(dispatched, Is.Zero);
        Assert.That(_publisher.Published, Has.Count.EqualTo(OutboxDispatcher.MaxDispatchAttempts));

        using var verifyScope = _provider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var message = verifyContext.OutboxMessages.Single();
        Assert.That(message.DispatchedAtUtc, Is.Null);
        Assert.That(message.DispatchAttempts, Is.EqualTo(OutboxDispatcher.MaxDispatchAttempts));
        Assert.That(message.LastError, Is.EqualTo("still down"));
    }

    [Test]
    public async Task DispatchBatchAsync_RecoveredPublisher_DispatchesPreviouslyFailedMessage()
    {
        SeedMessage("PolicyBound");
        _publisher.FailWith = new InvalidOperationException("transient");

        var dispatcher = BuildDispatcher();
        await dispatcher.DispatchBatchAsync(CancellationToken.None);

        _publisher.FailWith = null;
        var dispatched = await dispatcher.DispatchBatchAsync(CancellationToken.None);

        Assert.That(dispatched, Is.EqualTo(1));

        using var verifyScope = _provider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var message = verifyContext.OutboxMessages.Single();
        Assert.That(message.DispatchedAtUtc, Is.Not.Null);
        Assert.That(message.DispatchAttempts, Is.EqualTo(2));
        Assert.That(message.LastError, Is.Null);
    }

    private OutboxDispatcher BuildDispatcher()
    {
        return new OutboxDispatcher(
            _provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxDispatcher>.Instance,
            TimeProvider.System);
    }

    private void SeedMessage(string eventType)
    {
        using var seedScope = _provider.CreateScope();
        var context = seedScope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        context.OutboxMessages.Add(new OutboxMessageEntity
        {
            EventId = Guid.CreateVersion7(),
            AggregateType = "Policy",
            AggregateId = Guid.CreateVersion7(),
            EventType = eventType,
            PayloadJson = "{}",
            OccurredAtUtc = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private sealed class RecordingPublisher : IOutboxPublisher
    {
        public List<Guid> Published { get; } = [];

        public Exception? FailWith { get; set; }

        public Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default)
        {
            Published.Add(message.EventId);
            return FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);
        }
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
