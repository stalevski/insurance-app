using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.FinalMessages.Risks;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IRiskFlowService
{
    FinalRiskResponse Process(CanonicalRiskRequest request);
}
