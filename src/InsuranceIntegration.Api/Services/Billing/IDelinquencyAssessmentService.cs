namespace InsuranceIntegration.Api.Services.Billing;

public interface IDelinquencyAssessmentService
{
    DelinquencyAssessmentResult Assess(DelinquencyAssessmentRequest request);
}
