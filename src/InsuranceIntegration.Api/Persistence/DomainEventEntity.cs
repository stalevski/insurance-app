namespace InsuranceIntegration.Api.Persistence;

public sealed class DomainEventEntity
{
    public required Guid Id { get; init; }

    public required string EventType { get; init; }

    public required string AggregateKind { get; init; }

    public required string AggregateKey { get; init; }

    public required string Source { get; init; }

    public string? EnvelopeId { get; init; }

    public string? CorrelationId { get; init; }

    public required DateTime OccurredAtUtc { get; init; }

    public required DateTime RecordedAtUtc { get; init; }

    public int SchemaVersion { get; init; } = 1;

    public required string PayloadJson { get; init; }
}
