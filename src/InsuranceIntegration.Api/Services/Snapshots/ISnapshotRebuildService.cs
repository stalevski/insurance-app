using InsuranceIntegration.Api.Snapshots.Policies;
using InsuranceIntegration.Api.Snapshots.Quotes;

namespace InsuranceIntegration.Api.Services.Snapshots;

/// <summary>
/// Replays domain events for an aggregate and re-derives its snapshot in memory by
/// running the projector over each event's stored canonical request and final
/// response. Useful as a sanity check (compare against the live snapshot row) and
/// as a recovery tool if the snapshot was corrupted or schema changed.
/// </summary>
public interface ISnapshotRebuildService
{
    SnapshotRebuildResult<PolicySnapshot> RebuildPolicy(string policyReference);

    SnapshotRebuildResult<QuoteSnapshot> RebuildQuote(string quoteReference);
}
