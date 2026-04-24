using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly TimeProvider _timeProvider;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxDispatcher> logger,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started with poll interval {PollInterval}.", PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatch batch failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var pending = await context.OutboxMessages
            .Where(message => message.DispatchedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var dispatchedAt = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var message in pending)
        {
            _logger.LogInformation(
                "Dispatching outbox event {EventType} for {AggregateType} {AggregateId} (EventId={EventId}, CorrelationId={CorrelationId}).",
                message.EventType,
                message.AggregateType,
                message.AggregateId,
                message.EventId,
                message.CorrelationId);

            message.DispatchedAtUtc = dispatchedAt;
            message.DispatchAttempts += 1;
            message.LastError = null;
        }

        await context.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }
}
