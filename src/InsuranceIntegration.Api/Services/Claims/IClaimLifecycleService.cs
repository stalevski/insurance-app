namespace InsuranceIntegration.Api.Services.Claims;

public interface IClaimLifecycleService
{
    ClaimTransitionResult Transition(ClaimTransitionRequest request);
}
