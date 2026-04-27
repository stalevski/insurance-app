namespace InsuranceIntegration.Api.Services.Snapshots;

/// <summary>
/// Result of replaying domain events into a snapshot. Carries the rebuilt
/// snapshot together with metadata describing how many events were applied.
/// </summary>
public sealed class SnapshotRebuildResult<TSnapshot> where TSnapshot : class
{
    public required string AggregateKind { get; init; }

    public required string AggregateKey { get; init; }

    public required int EventsApplied { get; init; }

    public required DateTime? FirstEventAtUtc { get; init; }

    public required DateTime? LastEventAtUtc { get; init; }

    public TSnapshot? Snapshot { get; init; }
}
