namespace InsuranceIntegration.Api.Persistence;

public sealed class OutboxMessageEntity
{
    public Guid EventId { get; set; }

    public string AggregateType { get; set; } = string.Empty;

    public Guid AggregateId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public Guid? CorrelationId { get; set; }

    public Guid? CausationId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateTime? DispatchedAtUtc { get; set; }

    public int DispatchAttempts { get; set; }

    public string? LastError { get; set; }
}
