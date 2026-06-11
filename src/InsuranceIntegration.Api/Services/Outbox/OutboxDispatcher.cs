using InsuranceIntegration.Api.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceIntegration.Api.Services.Outbox;

public sealed class OutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;

    /// <summary>Messages that fail this many times are left pending for manual inspection (poison).</summary>
    public const int MaxDispatchAttempts = 5;

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
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

        var pending = await context.OutboxMessages
            .Where(message => message.DispatchedAtUtc == null && message.DispatchAttempts < MaxDispatchAttempts)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var dispatched = 0;
        foreach (var message in pending)
        {
            message.DispatchAttempts += 1;
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
                message.DispatchedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
                message.LastError = null;
                dispatched += 1;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                message.LastError = ex.Message;
                if (message.DispatchAttempts >= MaxDispatchAttempts)
                {
                    _logger.LogError(
                        ex,
                        "Outbox event {EventId} ({EventType}) failed attempt {Attempt}/{MaxAttempts} and is now poisoned; it will not be retried automatically.",
                        message.EventId,
                        message.EventType,
                        message.DispatchAttempts,
                        MaxDispatchAttempts);
                }
                else
                {
                    _logger.LogWarning(
                        ex,
                        "Outbox event {EventId} ({EventType}) failed attempt {Attempt}/{MaxAttempts}; will retry on a later poll.",
                        message.EventId,
                        message.EventType,
                        message.DispatchAttempts,
                        MaxDispatchAttempts);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return dispatched;
    }
}
