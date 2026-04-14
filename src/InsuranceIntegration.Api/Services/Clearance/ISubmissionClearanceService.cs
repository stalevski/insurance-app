using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Clearance;

public interface ISubmissionClearanceService
{
    SubmissionClearanceResult Evaluate(CanonicalRiskRequest request);
}
