using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;

namespace InsuranceIntegration.Api.Services.Snapshots;

public interface IRiskSnapshotRouter
{
    void Route(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context);
}
