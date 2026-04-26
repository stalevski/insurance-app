using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IRiskFlowService
{
    FinalRiskResponse Process(CanonicalRiskRequest request);
}
