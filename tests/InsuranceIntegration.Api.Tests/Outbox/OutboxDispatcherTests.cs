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

    public OutboxDispatcherTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<IntegrationDbContext>(options => options.UseSqlite(_connection));
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        context.Database.EnsureCreated();
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

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}
