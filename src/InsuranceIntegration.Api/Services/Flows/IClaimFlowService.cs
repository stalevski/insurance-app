using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Responses.Claims;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IClaimFlowService
{
    FinalClaimResponse Process(CanonicalClaimRequest request);
}
