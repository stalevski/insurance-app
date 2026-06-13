using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Matching;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class RiskFlowTransactionTypeTests
{
    private static RiskFlowService CreateService(ISubmissionRegistry? registry = null)
    {
        var calculator = new LevenshteinDistanceCalculator();
        var submissionRegistry = registry ?? new InMemorySubmissionRegistry();
        var clearanceService = new SubmissionClearanceService(submissionRegistry, calculator);
        return new RiskFlowService(clearanceService, submissionRegistry);
    }

    [TestCase(PolicyTransactionType.NewBusiness, PolicyStatusValue.ReadyToBind)]
    [TestCase(PolicyTransactionType.Renewal, PolicyStatusValue.Renewed)]
    [TestCase(PolicyTransactionType.MidTermAdjustment, PolicyStatusValue.Endorsed)]
    [TestCase(PolicyTransactionType.Cancellation, PolicyStatusValue.Cancelled)]
    [TestCase(PolicyTransactionType.Reinstatement, PolicyStatusValue.Reinstated)]
    [TestCase(PolicyTransactionType.Lapse, PolicyStatusValue.Lapsed)]
    [TestCase(PolicyTransactionType.NonRenewal, PolicyStatusValue.NonRenewed)]
    public void Process_ReturnsExpectedPolicyStatusForPolicyTransactionType(string transactionType, string expectedPolicyStatus)
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            underwritingYear: DateTime.UtcNow.Year,
            transactionType: transactionType);

        var result = service.Process(request);

        Assert.That(result.PolicyStatus, Is.EqualTo(expectedPolicyStatus));
    }

    [Test]
    public void Process_SetsPolicyStatusToBoundForBindTransaction()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            underwritingYear: DateTime.UtcNow.Year,
            transactionType: QuoteTransactionType.Bind,
            policyReference: "POL-BOUND-001");

        var result = service.Process(request);

        Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Bound));
    }

    [Test]
    public void Process_BindEventWithoutInsuredProfile_StillProducesBoundAndAutoCleared()
    {
        // Real-world bind events from BindPoint do not re-supply the full insured profile
        // (no YearsInBusiness, etc.). They must not be re-underwritten; the bind only needs
        // valid broker authority, a policy reference, a premium and complete contract/compliance checks.
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            underwritingYear: DateTime.UtcNow.Year,
            transactionType: "PolicyBind",
            yearsInBusiness: 0,
            insuredName: string.Empty,
            preferredBroker: true,
            delegatedAuthority: true,
            policyReference: "POL-REAL-001");

        var result = service.Process(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Bound));
            Assert.That(result.ClearanceDecision, Is.EqualTo("AutoCleared"));
            Assert.That(result.AutoCleared, Is.True);
            Assert.That(result.FinalStatus, Is.EqualTo("ReadyForDownstreamDispatch"));
        });
    }

    [Test]
    public void Process_SetsQuoteStatusToIndicativeForQuotableTransaction()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            underwritingYear: DateTime.UtcNow.Year,
            transactionType: QuoteTransactionType.Quotable);

        var result = service.Process(request);

        Assert.That(result.QuoteStatus, Is.EqualTo(QuoteStatusValue.Indicative));
    }

    [Test]
    public void Process_SetsQuoteStatusToQuotedForQuotedTransaction()
    {
        var service = CreateService();
        var request = TestRiskRequestFactory.Create(
            underwritingYear: DateTime.UtcNow.Year,
            transactionType: QuoteTransactionType.Quoted);

        var result = service.Process(request);

        Assert.That(result.QuoteStatus, Is.EqualTo(QuoteStatusValue.Quoted));
    }
}
