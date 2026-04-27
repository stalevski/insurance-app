namespace InsuranceIntegration.Api.Services.Policies;

/// <summary>
/// Generates a renewal quote for an existing bound policy. Computes the loss-ratio
/// and exposure-driven premium, marks the prior policy as Renewed, and creates a
/// fresh QuoteSnapshot for the new term. All side-effects (snapshot mutations and
/// domain events) are written within a single EF transaction per snapshot routing.
/// </summary>
public interface IPolicyRenewalService
{
    RenewalResult ApplyRenewal(RenewalRequest request);
}
