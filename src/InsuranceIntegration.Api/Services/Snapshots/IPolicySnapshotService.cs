using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Services.Snapshots;

public interface IPolicySnapshotService
{
    PolicySnapshot? Find(string policyReference);

    IReadOnlyList<PolicySnapshotSummary> List(int skip = 0, int take = 100);

    PolicySnapshot Apply(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context);
}

public sealed class PolicySnapshotSummary
{
    public string PolicyReference { get; init; } = string.Empty;

    public string? QuoteReference { get; init; }

    public string ProductCode { get; init; } = string.Empty;

    public int UnderwritingYear { get; init; }

    public string CurrentPhase { get; init; } = string.Empty;

    public DateTime LastUpdatedUtc { get; init; }

    public string Self { get; init; } = string.Empty;
}
