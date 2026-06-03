using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

/// <summary>
/// Resolves the applicable <see cref="IRiskTypeProfile"/> for a request and exposes
/// a convenience that derives the line-of-business enrichments directly.
/// </summary>
public interface IRiskProfileResolver
{
    IRiskTypeProfile? Resolve(CanonicalRiskRequest request);

    IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request);
}
