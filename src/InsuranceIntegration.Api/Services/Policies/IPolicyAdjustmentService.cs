namespace InsuranceIntegration.Api.Services.Policies;

public interface IPolicyAdjustmentService
{
    CancellationResult CalculateCancellation(CancellationRequest request);

    EndorsementResult CalculateEndorsement(EndorsementRequest request);
}
