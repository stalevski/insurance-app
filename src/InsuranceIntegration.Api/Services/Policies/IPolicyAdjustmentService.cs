namespace InsuranceIntegration.Api.Services.Policies;

public interface IPolicyAdjustmentService
{
    CancellationResult CalculateCancellation(CancellationRequest request);

    EndorsementResult CalculateEndorsement(EndorsementRequest request);

    ReinstatementResult CalculateReinstatement(ReinstatementRequest request);

    LapseResult CalculateLapse(LapseRequest request);

    NonRenewalResult CalculateNonRenewal(NonRenewalRequest request);
}
