using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;

namespace InsuranceIntegration.Api.Services.Orchestration;

public interface IRiskSubmissionOrchestrator
{
    Task<FinalRiskResponse> HandleAsync(
        CanonicalRiskRequest request,
        IngestContext context,
        CancellationToken cancellationToken = default);
}
