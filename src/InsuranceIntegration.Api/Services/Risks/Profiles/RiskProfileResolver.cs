using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Products;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

public sealed class RiskProfileResolver : IRiskProfileResolver
{
    private readonly ILineOfBusinessResolver _lineOfBusinessResolver;
    private readonly IReadOnlyDictionary<LineOfBusiness, IRiskTypeProfile> _profiles;

    public RiskProfileResolver(
        ILineOfBusinessResolver lineOfBusinessResolver,
        IEnumerable<IRiskTypeProfile> profiles)
    {
        _lineOfBusinessResolver = lineOfBusinessResolver;
        _profiles = profiles.ToDictionary(profile => profile.LineOfBusiness);
    }

    /// <summary>
    /// Builds a resolver with the built-in profiles and product catalog. Used as a
    /// default when the service is constructed outside dependency injection (e.g. tests).
    /// </summary>
    public static RiskProfileResolver CreateDefault()
    {
        return new RiskProfileResolver(
            new LineOfBusinessResolver(new ProductCatalog()),
            new IRiskTypeProfile[]
            {
                new PropertyRiskProfile(),
                new LiabilityRiskProfile(),
                new CyberRiskProfile(),
                new MotorRiskProfile()
            });
    }

    public IRiskTypeProfile? Resolve(CanonicalRiskRequest request)
    {
        var lineOfBusiness = request.LineOfBusiness ?? _lineOfBusinessResolver.Resolve(request.ProductCode);
        return _profiles.GetValueOrDefault(lineOfBusiness);
    }

    public IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        return Resolve(request)?.DeriveEnrichments(request) ?? [];
    }
}
