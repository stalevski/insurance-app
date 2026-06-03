using InsuranceIntegration.Api.CanonicalContracts.Risks;

namespace InsuranceIntegration.Api.Services.Risks.Profiles;

public sealed class CyberRiskProfile : IRiskTypeProfile
{
    private const long LargeDataHolderThreshold = 1_000_000;

    public LineOfBusiness LineOfBusiness => LineOfBusiness.Cyber;

    public IReadOnlyCollection<EnrichmentItem> DeriveEnrichments(CanonicalRiskRequest request)
    {
        var details = request.CyberDetails;
        var ransomwareControlsMissing = details?.RansomwareControlsInPlace == false;

        var enrichments = new List<EnrichmentItem>
        {
            new() { Family = "Cyber", Code = "ATTACK_SURFACE", Description = "External attack surface review", Multiplier = 1.08m, IsDerived = true, IsBlocking = false },
            new() { Family = "Cyber", Code = "RANSOMWARE_CONTROLS", Description = "Ransomware controls check", Multiplier = 1.05m, IsDerived = true, IsBlocking = ransomwareControlsMissing }
        };

        if (details?.MultiFactorAuthentication == false)
        {
            enrichments.Add(new EnrichmentItem { Family = "Cyber", Code = "MFA_GAP", Description = "Multi-factor authentication not enforced", Multiplier = 1.10m, IsDerived = true, IsBlocking = true });
        }

        if (details?.PriorBreach == true)
        {
            enrichments.Add(new EnrichmentItem { Family = "Cyber", Code = "PRIOR_BREACH", Description = "Prior cyber breach disclosed", Multiplier = 1.15m, IsDerived = true, IsBlocking = false });
        }

        if (details?.SensitiveRecordsHeld is { } records && records >= LargeDataHolderThreshold)
        {
            enrichments.Add(new EnrichmentItem { Family = "Cyber", Code = "LARGE_DATA_HOLDER", Description = "Large volume of sensitive records held", Multiplier = 1.06m, IsDerived = true, IsBlocking = false });
        }

        return enrichments;
    }
}
