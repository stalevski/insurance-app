using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Products;
using InsuranceIntegration.Api.Services.Risks;
using InsuranceIntegration.Api.Services.Risks.Profiles;

namespace InsuranceIntegration.Api.Tests.Risks;

public sealed class RiskProfileTests
{
    private static CanonicalRiskRequest CreateRequest(
        string productCode,
        LineOfBusiness? lineOfBusiness = null,
        PropertyRiskDetails? property = null,
        CyberRiskDetails? cyber = null,
        MotorRiskDetails? motor = null,
        LiabilityRiskDetails? liability = null,
        decimal? insuredRevenue = null)
    {
        return new CanonicalRiskRequest
        {
            ExternalReference = "REF-1",
            ProductCode = productCode,
            SourceSystem = "TEST",
            TransactionType = "Submission",
            LineOfBusiness = lineOfBusiness,
            PropertyDetails = property,
            CyberDetails = cyber,
            MotorDetails = motor,
            LiabilityDetails = liability,
            Insured = new InsuredData { AnnualRevenue = insuredRevenue }
        };
    }

    private static List<string> Codes(IReadOnlyCollection<EnrichmentItem> items)
    {
        return items.Select(item => item.Code).ToList();
    }

    [Test]
    public void LineOfBusinessResolver_MapsCatalogProductsByFamily()
    {
        var resolver = new LineOfBusinessResolver(new ProductCatalog());

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Resolve("COMMERCIAL_PROPERTY"), Is.EqualTo(LineOfBusiness.Property));
            Assert.That(resolver.Resolve("LIABILITY"), Is.EqualTo(LineOfBusiness.Liability));
            Assert.That(resolver.Resolve("CYBER"), Is.EqualTo(LineOfBusiness.Cyber));
            Assert.That(resolver.Resolve("MOTOR"), Is.EqualTo(LineOfBusiness.Motor));
        });
    }

    [Test]
    public void LineOfBusinessResolver_FallsBackToKeywordsForUnknownCodes()
    {
        var resolver = new LineOfBusinessResolver(new ProductCatalog());

        Assert.Multiple(() =>
        {
            Assert.That(resolver.Resolve("FLEET_AUTO_2026"), Is.EqualTo(LineOfBusiness.Motor));
            Assert.That(resolver.Resolve("HOME_PROPERTY_PLUS"), Is.EqualTo(LineOfBusiness.Property));
            Assert.That(resolver.Resolve("CYBER_PRO"), Is.EqualTo(LineOfBusiness.Cyber));
            Assert.That(resolver.Resolve("PROFESSIONAL_INDEMNITY"), Is.EqualTo(LineOfBusiness.Liability));
            Assert.That(resolver.Resolve("WIDGET"), Is.EqualTo(LineOfBusiness.Unknown));
        });
    }

    [Test]
    public void PropertyProfile_BaselineEnrichmentsAreNotBlocking()
    {
        var profile = new PropertyRiskProfile();

        var result = profile.DeriveEnrichments(CreateRequest("COMMERCIAL_PROPERTY"));

        Assert.That(Codes(result), Is.EquivalentTo(new[] { "GEO_CAT", "BUILDING_PROFILE" }));
        Assert.That(result.Any(item => item.IsBlocking), Is.False);
    }

    [Test]
    public void PropertyProfile_FlagsAgingConstructionFloodAndSprinklerGap()
    {
        var profile = new PropertyRiskProfile();
        var request = CreateRequest(
            "COMMERCIAL_PROPERTY",
            property: new PropertyRiskDetails { YearBuilt = 1965, FloodZone = "AE", Sprinklered = false });

        var result = profile.DeriveEnrichments(request);

        Assert.Multiple(() =>
        {
            Assert.That(Codes(result), Does.Contain("SPRINKLER_GAP"));
            Assert.That(Codes(result), Does.Contain("FLOOD_EXPOSURE"));
            Assert.That(result.Single(item => item.Code == "BUILDING_PROFILE").IsBlocking, Is.True);
        });
    }

    [Test]
    public void CyberProfile_BlocksWhenMfaMissingOrRansomwareControlsAbsent()
    {
        var profile = new CyberRiskProfile();
        var request = CreateRequest(
            "CYBER",
            cyber: new CyberRiskDetails
            {
                MultiFactorAuthentication = false,
                RansomwareControlsInPlace = false,
                PriorBreach = true,
                SensitiveRecordsHeld = 2_000_000
            });

        var result = profile.DeriveEnrichments(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Single(item => item.Code == "RANSOMWARE_CONTROLS").IsBlocking, Is.True);
            Assert.That(result.Single(item => item.Code == "MFA_GAP").IsBlocking, Is.True);
            Assert.That(Codes(result), Does.Contain("PRIOR_BREACH"));
            Assert.That(Codes(result), Does.Contain("LARGE_DATA_HOLDER"));
        });
    }

    [Test]
    public void CyberProfile_WithStrongControlsHasNoBlockingEnrichments()
    {
        var profile = new CyberRiskProfile();
        var request = CreateRequest(
            "CYBER",
            cyber: new CyberRiskDetails { MultiFactorAuthentication = true, RansomwareControlsInPlace = true });

        var result = profile.DeriveEnrichments(request);

        Assert.That(result.Any(item => item.IsBlocking), Is.False);
    }

    [Test]
    public void MotorProfile_AddsYoungDriverAndLargeFleetLoads()
    {
        var profile = new MotorRiskProfile();
        var request = CreateRequest(
            "MOTOR",
            motor: new MotorRiskDetails { YoungestDriverAge = 19, FleetSize = 40 });

        var result = profile.DeriveEnrichments(request);

        Assert.That(Codes(result), Is.SupersetOf(new[] { "DRIVING_HISTORY", "PAYMENT_HISTORY", "YOUNG_DRIVER", "LARGE_FLEET" }));
    }

    [Test]
    public void LiabilityProfile_BlocksFinancialHealthOnLowRevenueOrPriorClaims()
    {
        var profile = new LiabilityRiskProfile();

        var lowRevenue = profile.DeriveEnrichments(CreateRequest("LIABILITY", insuredRevenue: 50_000m));
        var priorClaims = profile.DeriveEnrichments(
            CreateRequest("LIABILITY", liability: new LiabilityRiskDetails { HasPriorClaims = true }, insuredRevenue: 500_000m));
        var healthy = profile.DeriveEnrichments(CreateRequest("LIABILITY", insuredRevenue: 500_000m));

        Assert.Multiple(() =>
        {
            Assert.That(lowRevenue.Single(item => item.Code == "FINANCIAL_HEALTH").IsBlocking, Is.True);
            Assert.That(priorClaims.Single(item => item.Code == "FINANCIAL_HEALTH").IsBlocking, Is.True);
            Assert.That(healthy.Single(item => item.Code == "FINANCIAL_HEALTH").IsBlocking, Is.False);
        });
    }

    [Test]
    public void RiskProfileResolver_SelectsProfileByDerivedLineOfBusiness()
    {
        var resolver = RiskProfileResolver.CreateDefault();

        var cyber = resolver.Resolve(CreateRequest("CYBER"));
        var motor = resolver.Resolve(CreateRequest("MOTOR"));
        var unknown = resolver.Resolve(CreateRequest("WIDGET"));

        Assert.Multiple(() =>
        {
            Assert.That(cyber, Is.TypeOf<CyberRiskProfile>());
            Assert.That(motor, Is.TypeOf<MotorRiskProfile>());
            Assert.That(unknown, Is.Null);
        });
    }

    [Test]
    public void RiskProfileResolver_HonoursExplicitLineOfBusinessOverride()
    {
        var resolver = RiskProfileResolver.CreateDefault();

        // Product code would resolve to Property, but explicit override wins.
        var profile = resolver.Resolve(CreateRequest("COMMERCIAL_PROPERTY", lineOfBusiness: LineOfBusiness.Cyber));

        Assert.That(profile, Is.TypeOf<CyberRiskProfile>());
    }

    [Test]
    public void RiskProfileResolver_ReturnsNoEnrichmentsForUnknownLineOfBusiness()
    {
        var resolver = RiskProfileResolver.CreateDefault();

        var result = resolver.DeriveEnrichments(CreateRequest("WIDGET"));

        Assert.That(result, Is.Empty);
    }
}
