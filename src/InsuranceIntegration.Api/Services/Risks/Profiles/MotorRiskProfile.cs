using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

public sealed class MotorRiskProfile : IRiskTypeProfile
{
    private const int YoungDriverAgeThreshold = 25;
    private const int LargeFleetThreshold = 25;

    public LineOfBusiness LineOfBusiness => LineOfBusiness.Motor;

    public IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        var details = request.MotorDetails;

        var enrichments = new List<EnrichmentItem>
        {
            new() { Family = "Motor", Code = "DRIVING_HISTORY", Description = "Driving history review", Multiplier = 1.03m, IsDerived = true, IsBlocking = false },
            new() { Family = "Motor", Code = "PAYMENT_HISTORY", Description = "Payment history check", Multiplier = 1.02m, IsDerived = true, IsBlocking = false }
        };

        if (details?.YoungestDriverAge is { } age && age < YoungDriverAgeThreshold)
        {
            enrichments.Add(new EnrichmentItem { Family = "Motor", Code = "YOUNG_DRIVER", Description = "Young driver on policy", Multiplier = 1.06m, IsDerived = true, IsBlocking = false });
        }

        if (details?.FleetSize is { } fleetSize && fleetSize >= LargeFleetThreshold)
        {
            enrichments.Add(new EnrichmentItem { Family = "Motor", Code = "LARGE_FLEET", Description = "Large fleet exposure", Multiplier = 1.04m, IsDerived = true, IsBlocking = false });
        }

        return enrichments;
    }
}
