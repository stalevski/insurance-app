using InsuranceIntegration.Api.CanonicalContracts.Compliance;
using InsuranceIntegration.Api.FinalMessages.Compliance;

namespace InsuranceIntegration.Api.Services.Flows;

public interface IComplianceFlowService
{
    FinalComplianceResponse Process(CanonicalComplianceRequest request);
}
