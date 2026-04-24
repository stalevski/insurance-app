using System.Text.Json;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Correlation;

namespace InsuranceIntegration.Api.Services.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IntegrationDbContext _context;
    private readonly ICorrelationContext _correlationContext;
    private readonly TimeProvider _timeProvider;

    public OutboxWriter(
        IntegrationDbContext context,
        ICorrelationContext correlationContext,
        TimeProvider timeProvider)
    {
        _context = context;
        _correlationContext = correlationContext;
        _timeProvider = timeProvider;
    }

    public void Enqueue<TPayload>(string aggregateType, Guid aggregateId, string eventType, TPayload payload)
    {
        var message = new OutboxMessageEntity
        {
            EventId = Guid.CreateVersion7(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload, SerializerOptions),
            CorrelationId = _correlationContext.CorrelationId,
            CausationId = _correlationContext.CausationId,
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            DispatchedAtUtc = null,
            DispatchAttempts = 0
        };

        _context.OutboxMessages.Add(message);
    }
}
