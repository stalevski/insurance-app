using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

public sealed class PropertyRiskProfile : IRiskTypeProfile
{
    private const int AgingConstructionYearThreshold = 1980;

    public LineOfBusiness LineOfBusiness => LineOfBusiness.Property;

    public IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        var details = request.PropertyDetails;
        var agingConstruction = details?.YearBuilt is { } yearBuilt && yearBuilt < AgingConstructionYearThreshold;
        var highHazardFloodZone = IsHighHazardFloodZone(details?.FloodZone);

        var enrichments = new List<EnrichmentItem>
        {
            new() { Family = "Property", Code = "GEO_CAT", Description = "Geo-cat accumulation screening", Multiplier = 1.04m, IsDerived = true, IsBlocking = false },
            new() { Family = "Property", Code = "BUILDING_PROFILE", Description = "Building age and attribute review", Multiplier = 1.02m, IsDerived = true, IsBlocking = agingConstruction || highHazardFloodZone }
        };

        if (details?.Sprinklered == false)
        {
            enrichments.Add(new EnrichmentItem { Family = "Property", Code = "SPRINKLER_GAP", Description = "No automatic sprinkler protection", Multiplier = 1.05m, IsDerived = true, IsBlocking = false });
        }

        if (highHazardFloodZone)
        {
            enrichments.Add(new EnrichmentItem { Family = "Property", Code = "FLOOD_EXPOSURE", Description = "High-hazard flood zone exposure", Multiplier = 1.08m, IsDerived = true, IsBlocking = false });
        }

        return enrichments;
    }

    private static bool IsHighHazardFloodZone(string? floodZone)
    {
        if (string.IsNullOrWhiteSpace(floodZone))
        {
            return false;
        }

        var zone = floodZone.Trim().ToUpperInvariant();
        return zone.StartsWith('A') || zone.StartsWith('V');
    }
}
