using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.FinalMessages.Claims;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IClaimFlowService
{
    FinalClaimResponse Process(CanonicalClaimRequest request);
}
