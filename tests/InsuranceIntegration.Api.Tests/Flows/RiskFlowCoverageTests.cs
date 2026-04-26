using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class RiskFlowCoverageTests
{
    private static RiskFlowService CreateService()
    {
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new InMemorySubmissionRegistry();
        var clearance = new SubmissionClearanceService(registry, calculator);
        return new RiskFlowService(clearance, registry);
    }

    [Test]
    public void Process_SumsSumInsuredAcrossActiveSectionsAndIgnoresRemoved()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create();

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "PROP",
                SectionName = "Property",
                Status = SectionStatus.Active,
                SumInsured = 750000m,
                SectionPremium = 600m,
                Subcovers = [ new SubcoverData { SubcoverCode = "FIRE", SubcoverName = "Fire", SumInsured = 750000m, Premium = 600m } ]
            },
            new()
            {
                SectionCode = "BI",
                SectionName = "Business Interruption",
                Status = SectionStatus.Active,
                SumInsured = 250000m,
                SectionPremium = 400m,
                Subcovers = [ new SubcoverData { SubcoverCode = "BI-12", SubcoverName = "12 month BI", SumInsured = 250000m, Premium = 400m } ]
            },
            new()
            {
                SectionCode = "OLD",
                SectionName = "Removed Section",
                Status = SectionStatus.Removed,
                SumInsured = 999999m,
                SectionPremium = 999m
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.TotalSumInsured, Is.EqualTo(1_000_000m));
        Assert.That(result.TotalSectionPremium, Is.EqualTo(1000m));
    }

    [Test]
    public void Process_PremiumAllocationBalancedWhenSectionPremiumMatchesBaseWithinTolerance()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(brokerPremium: 1000m);

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "PROP",
                SectionName = "Property",
                Status = SectionStatus.Active,
                SumInsured = 500000m,
                SectionPremium = 1005m,
                Subcovers =
                [
                    new SubcoverData { SubcoverCode = "FIRE", SubcoverName = "Fire", SumInsured = 500000m, Premium = 1005m }
                ]
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.PremiumAllocationBalanced, Is.True);
    }

    [Test]
    public void Process_PremiumAllocationUnbalancedWhenSectionPremiumDriftsBeyondTolerance()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(brokerPremium: 1000m);

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "PROP",
                SectionName = "Property",
                Status = SectionStatus.Active,
                SumInsured = 500000m,
                SectionPremium = 750m,
                Subcovers =
                [
                    new SubcoverData { SubcoverCode = "FIRE", SubcoverName = "Fire", SumInsured = 500000m, Premium = 750m }
                ]
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.PremiumAllocationBalanced, Is.False);
    }

    [Test]
    public void Process_RaisesCoverageWarningWhenDeductibleExceedsSumInsured()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create();

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "PROP",
                SectionName = "Property",
                SumInsured = 100_000m,
                SectionPremium = 500m,
                Subcovers =
                [
                    new SubcoverData
                    {
                        SubcoverCode = "FIRE",
                        SubcoverName = "Fire",
                        SumInsured = 100_000m,
                        Deductible = 250_000m,
                        Premium = 500m
                    }
                ]
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.CoverageWarnings, Has.Some.Contains("deductible"));
        Assert.That(result.AppliedEnrichments, Does.Contain("Coverage:STRUCTURE_WARNING"));
    }

    [Test]
    public void Process_RaisesCoverageWarningWhenAggregateLimitBelowPerOccurrence()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create();

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "LIAB",
                SectionName = "Liability",
                SumInsured = 1_000_000m,
                SectionPremium = 500m,
                Subcovers =
                [
                    new SubcoverData
                    {
                        SubcoverCode = "GL",
                        SubcoverName = "General Liability",
                        SumInsured = 1_000_000m,
                        PerOccurrenceLimit = 1_000_000m,
                        AggregateLimit = 500_000m,
                        Premium = 500m
                    }
                ]
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.CoverageWarnings, Has.Some.Contains("aggregate limit"));
    }

    [Test]
    public void Process_BlocksWhenClaimReferencesExcludedPeril()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(claimCount: 1);

        var enriched = WithClaimsAndSections(request,
            claims: [ new ClaimData
            {
                ClaimReference = "CLM-1",
                ClaimantName = "Northwind Storage Ltd",
                IncurredAmount = 1000m,
                ReservedAmount = 200m,
                AffectedSectionCode = "PROP",
                AffectedSubcoverCode = "FIRE",
                AffectedPerilCode = "FLOOD"
            } ],
            sections:
            [
                new SectionData
                {
                    SectionCode = "PROP",
                    SectionName = "Property",
                    SumInsured = 500_000m,
                    SectionPremium = 1000m,
                    Subcovers =
                    [
                        new SubcoverData
                        {
                            SubcoverCode = "FIRE",
                            SubcoverName = "Fire",
                            SumInsured = 500_000m,
                            Premium = 1000m,
                            Exclusions = [ "FLOOD" ],
                            Perils = [ new PerilData { Code = "FIRE", Name = "Fire", IsCovered = true } ]
                        }
                    ]
                }
            ]);

        var result = service.Process(enriched);

        Assert.That(result.AppliedEnrichments, Does.Contain("Coverage:UNCOVERED_PERIL_CLAIM"));
        Assert.That(result.QuoteStatus, Is.EqualTo("Blocked"));
        Assert.That(result.InsuredDecision, Is.EqualTo("Decline"));
    }

    [Test]
    public void Process_BlocksWhenClaimReferencesPerilMarkedNotCovered()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(claimCount: 1);

        var enriched = WithClaimsAndSections(request,
            claims: [ new ClaimData
            {
                ClaimReference = "CLM-2",
                ClaimantName = "Northwind Storage Ltd",
                IncurredAmount = 1000m,
                AffectedSectionCode = "PROP",
                AffectedSubcoverCode = "FIRE",
                AffectedPerilCode = "EARTHQUAKE"
            } ],
            sections:
            [
                new SectionData
                {
                    SectionCode = "PROP",
                    SectionName = "Property",
                    SumInsured = 500_000m,
                    Subcovers =
                    [
                        new SubcoverData
                        {
                            SubcoverCode = "FIRE",
                            SubcoverName = "Fire",
                            SumInsured = 500_000m,
                            Perils =
                            [
                                new PerilData { Code = "FIRE", Name = "Fire", IsCovered = true },
                                new PerilData { Code = "EARTHQUAKE", Name = "Earthquake", IsCovered = false }
                            ]
                        }
                    ]
                }
            ]);

        var result = service.Process(enriched);

        Assert.That(result.AppliedEnrichments, Does.Contain("Coverage:UNCOVERED_PERIL_CLAIM"));
    }

    [Test]
    public void Process_DoesNotBlockWhenClaimPerilIsExplicitlyCovered()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(claimCount: 1, incurredPerClaim: 500m);

        var enriched = WithClaimsAndSections(request,
            claims: [ new ClaimData
            {
                ClaimReference = "CLM-3",
                ClaimantName = "Northwind Storage Ltd",
                IncurredAmount = 500m,
                ReservedAmount = 100m,
                AffectedSectionCode = "PROP",
                AffectedSubcoverCode = "FIRE",
                AffectedPerilCode = "FIRE"
            } ],
            sections:
            [
                new SectionData
                {
                    SectionCode = "PROP",
                    SectionName = "Property",
                    SumInsured = 500_000m,
                    SectionPremium = 1000m,
                    Subcovers =
                    [
                        new SubcoverData
                        {
                            SubcoverCode = "FIRE",
                            SubcoverName = "Fire",
                            SumInsured = 500_000m,
                            Premium = 1000m,
                            Perils = [ new PerilData { Code = "FIRE", Name = "Fire", IsCovered = true } ]
                        }
                    ]
                }
            ]);

        var result = service.Process(enriched);

        Assert.That(result.AppliedEnrichments, Does.Not.Contain("Coverage:UNCOVERED_PERIL_CLAIM"));
    }

    [Test]
    public void Process_FlagsUnrecognizedDeductibleType()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create();

        var enriched = WithSections(request, new List<SectionData>
        {
            new()
            {
                SectionCode = "PROP",
                SectionName = "Property",
                SumInsured = 100_000m,
                Subcovers =
                [
                    new SubcoverData
                    {
                        SubcoverCode = "FIRE",
                        SubcoverName = "Fire",
                        SumInsured = 100_000m,
                        DeductibleType = "Bizarre"
                    }
                ]
            }
        });

        var result = service.Process(enriched);

        Assert.That(result.CoverageWarnings, Has.Some.Contains("unrecognized deductible type"));
    }

    private static CanonicalRiskRequest WithSections(CanonicalRiskRequest source, List<SectionData> sections)
    {
        return CloneWith(source, claims: source.Claims, sections: sections);
    }

    private static CanonicalRiskRequest WithClaimsAndSections(CanonicalRiskRequest source, List<ClaimData> claims, List<SectionData> sections)
    {
        return CloneWith(source, claims: claims, sections: sections);
    }

    private static CanonicalRiskRequest CloneWith(CanonicalRiskRequest source, List<ClaimData> claims, List<SectionData> sections)
    {
        return new CanonicalRiskRequest
        {
            EntityId = source.EntityId,
            ExternalReference = source.ExternalReference,
            ProductCode = source.ProductCode,
            SourceSystem = source.SourceSystem,
            TransactionType = source.TransactionType,
            SchemeCode = source.SchemeCode,
            TransactionTimestampUtc = source.TransactionTimestampUtc,
            BoundDate = source.BoundDate,
            LifecycleStatus = source.LifecycleStatus,
            AnnualizedGrossPremium = source.AnnualizedGrossPremium,
            CurrencyCode = source.CurrencyCode,
            UnderwriterName = source.UnderwriterName,
            PaymentMethod = source.PaymentMethod,
            Submission = source.Submission,
            Broker = source.Broker,
            Insured = source.Insured,
            Quote = source.Quote,
            Policy = source.Policy,
            Clearance = source.Clearance,
            Enrichments = source.Enrichments,
            ContractChecks = source.ContractChecks,
            ComplianceChecks = source.ComplianceChecks,
            Parties = source.Parties,
            Claims = claims,
            Sections = sections,
            SectionOperations = source.SectionOperations,
            Installments = source.Installments
        };
    }
}
