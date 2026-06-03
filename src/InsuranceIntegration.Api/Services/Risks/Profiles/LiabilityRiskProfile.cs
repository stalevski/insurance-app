using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

public sealed class LiabilityRiskProfile : IRiskTypeProfile
{
    private const decimal LowRevenueThreshold = 100_000m;

    public LineOfBusiness LineOfBusiness => LineOfBusiness.Liability;

    public IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        var lowRevenue = request.Insured.AnnualRevenue is < LowRevenueThreshold;
        var priorClaims = request.LiabilityDetails?.HasPriorClaims == true;

        return new List<EnrichmentItem>
        {
            new() { Family = "Liability", Code = "SANCTIONS_SCREEN", Description = "Sanctions screening review", Multiplier = 1.01m, IsDerived = true, IsBlocking = false },
            new() { Family = "Liability", Code = "FINANCIAL_HEALTH", Description = "Financial health review", Multiplier = 1.03m, IsDerived = true, IsBlocking = lowRevenue || priorClaims }
        };
    }
}
