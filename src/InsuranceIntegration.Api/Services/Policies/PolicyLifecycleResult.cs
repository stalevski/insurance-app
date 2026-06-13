namespace InsuranceIntegration.Api.Services.Policies;

/// <summary>
/// Outcome of a post-bind policy lifecycle operation (cancellation or endorsement).
/// Combines the financial calculation with the resulting snapshot status and the
/// id of the domain event recorded in the same transaction.
/// </summary>
public sealed class PolicyLifecycleResult
{
    public required string PolicyReference { get; init; }

    public required string TransactionType { get; init; }

    public required string PolicyStatus { get; init; }

    public required string CurrentPhase { get; init; }

    public required Guid DomainEventId { get; init; }

    public required string DomainEventType { get; init; }

    public CancellationResult? Cancellation { get; init; }

    public EndorsementResult? Endorsement { get; init; }

    public ReinstatementResult? Reinstatement { get; init; }
}
