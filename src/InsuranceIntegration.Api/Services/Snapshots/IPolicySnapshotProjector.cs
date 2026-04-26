using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Snapshots.Policies;

namespace InsuranceIntegration.Api.Services.Snapshots;

public interface IPolicySnapshotProjector
{
    PolicySnapshot Apply(
        PolicySnapshot? current,
        CanonicalRiskRequest request,
        FinalRiskResponse response,
        IngestContext context);
}
