using System.Text.Json;
using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Wire shape written/POSTed by the file and webhook transports. The original domain-event payload
/// is embedded as nested JSON under <see cref="Payload"/>.
/// </summary>
public sealed class OutboxEventEnvelope
{
    public Guid EventId { get; set; }

    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public JsonElement? Payload { get; set; }

    /// <summary>Projects a persisted outbox row onto the wire envelope, parsing the stored payload JSON.</summary>
    public static OutboxEventEnvelope FromMessage(OutboxMessageEntity message)
    {
        ArgumentNullException.ThrowIfNull(message);

        JsonElement? payload = null;
        if (!string.IsNullOrWhiteSpace(message.PayloadJson))
        {
            using var document = JsonDocument.Parse(message.PayloadJson);
            payload = document.RootElement.Clone();
        }

        return new OutboxEventEnvelope
        {
            EventId = message.EventId,
            AggregateType = message.AggregateType,
            AggregateId = message.AggregateId,
            EventType = message.EventType,
            CorrelationId = message.CorrelationId,
            CausationId = message.CausationId,
            OccurredAtUtc = message.OccurredAtUtc,
            Payload = payload,
        };
    }
}
