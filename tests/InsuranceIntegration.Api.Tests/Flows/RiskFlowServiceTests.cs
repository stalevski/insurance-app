using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class RiskFlowServiceTests
{
    private static RiskFlowService CreateService(ISubmissionRegistry? registry = null)
    {
        var calculator = new LevenshteinDistanceCalculator();
        var submissionRegistry = registry ?? new InMemorySubmissionRegistry();
        var clearanceService = new SubmissionClearanceService(submissionRegistry, calculator);
        return new RiskFlowService(calculator, clearanceService, submissionRegistry);
    }

    [Test]
    public void Process_UsesBrokerPremiumBeforeTechnicalAndAnnualizedPremium()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(brokerPremium: 1200m, technicalPremium: 900m, annualizedPremium: 800m);

        var result = service.Process(request);

        Assert.That(result.BasePremium, Is.EqualTo(1200m));
        Assert.That(result.AdjustedPremium, Is.GreaterThan(result.BasePremium));
    }

    [Test]
    public void Process_AutoClearsWhenEligibilityRulesAreSatisfied()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            brokerPremium: 1000m,
            technicalPremium: 900m,
            annualizedPremium: 800m,
            productCode: "COMMERCIAL_PROPERTY",
            underwritingYear: DateTime.UtcNow.Year,
            insuredRevenue: 500000m,
            yearsInBusiness: 5,
            preferredBroker: true,
            autoClearanceEnabled: true,
            premiumThreshold: 5000m,
            fuzzyMatchTolerance: 3,
            claimCount: 1,
            incurredPerClaim: 500m,
            reservedPerClaim: 100m,
            checksComplete: true,
            subcoverCount: 1);

        var result = service.Process(request);

        Assert.That(result.ClearanceDecision, Is.EqualTo("AutoCleared"));
        Assert.That(result.AutoCleared, Is.True);
        Assert.That(result.InsuredDecision, Is.EqualTo("AcceptableInsured"));
        Assert.That(result.QuoteStatus, Is.EqualTo("Quoted"));
        Assert.That(result.PolicyStatus, Is.EqualTo("ReadyToBind"));
        Assert.That(result.FinalStatus, Is.EqualTo("ReadyForDownstreamDispatch"));
    }

    [Test]
    public void Process_BlocksQuoteAndForcesManualClearanceWhenClaimBurdenCreatesBlockingEnrichment()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            productCode: "COMMERCIAL_PROPERTY",
            underwritingYear: DateTime.UtcNow.Year,
            claimCount: 3,
            incurredPerClaim: 10000m,
            reservedPerClaim: 500m,
            checksComplete: true,
            subcoverCount: 4);

        var result = service.Process(request);

        Assert.That(result.InsuredDecision, Is.EqualTo("Decline"));
        Assert.That(result.QuoteStatus, Is.EqualTo("Blocked"));
        Assert.That(result.ClearanceDecision, Is.EqualTo("ManualClearance"));
        Assert.That(result.AutoCleared, Is.False);
        Assert.That(result.BlockingEnrichmentCount, Is.GreaterThanOrEqualTo(1));
        Assert.That(result.AppliedEnrichments, Does.Contain("Universal:CLAIM_HISTORY_WEIGHT"));
        Assert.That(result.AppliedEnrichments, Does.Contain("Universal:SECTION_COMPLEXITY"));
    }
}
