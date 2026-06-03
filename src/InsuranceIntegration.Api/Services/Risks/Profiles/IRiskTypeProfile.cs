using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

/// <summary>
/// A line-of-business specific risk strategy. Each profile derives the enrichments
/// that are meaningful for its line of business, replacing ad-hoc product-code
/// string matching with an extensible strategy per risk type.
/// </summary>
public interface IRiskTypeProfile
{
    LineOfBusiness LineOfBusiness { get; }

    IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request);
}
