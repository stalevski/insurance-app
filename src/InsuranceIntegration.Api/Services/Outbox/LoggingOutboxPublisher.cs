using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Default publisher used until a real transport (queue/webhook) is configured. Logs the event at
/// Information level and succeeds. Swap this registration for a transport-backed implementation
/// to deliver events to external consumers.
/// </summary>
public sealed class LoggingOutboxPublisher : IOutboxPublisher
{
    private readonly ILogger<LoggingOutboxPublisher> _logger;

    public LoggingOutboxPublisher(ILogger<LoggingOutboxPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Published outbox event {EventType} for {AggregateType} {AggregateId} (EventId={EventId}, CorrelationId={CorrelationId}).",
            message.EventType,
            message.AggregateType,
            message.AggregateId,
            message.EventId,
            message.CorrelationId);
        return Task.CompletedTask;
    }
}
