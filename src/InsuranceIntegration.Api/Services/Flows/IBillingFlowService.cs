using InsuranceIntegration.Api.CanonicalContracts.Billing;
using InsuranceIntegration.Api.FinalMessages.Billing;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IBillingFlowService
{
    FinalBillingResponse Process(CanonicalBillingRequest request);
}
