namespace InsuranceIntegration.Api.Services.Policies;

/// <summary>
/// Orchestrates post-bind policy operations (cancellation, endorsement). For each
/// operation: computes the financial math, synthesizes a canonical risk request
/// from the existing snapshot, runs it through the standard risk flow + snapshot
/// router, and returns the combined result. The PolicySnapshot mutation and the
/// DomainEvent row are written in the same EF transaction.
/// </summary>
public interface IPolicyLifecycleService
{
    PolicyLifecycleResult ApplyCancellation(CancellationRequest request);

    PolicyLifecycleResult ApplyEndorsement(EndorsementRequest request);

    PolicyLifecycleResult ApplyReinstatement(ReinstatementRequest request);

    PolicyLifecycleResult ApplyLapse(LapseRequest request);

    PolicyLifecycleResult ApplyNonRenewal(NonRenewalRequest request);
}
