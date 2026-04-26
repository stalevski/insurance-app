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
            transactionType: QuoteTransactionType.Bind);

        var result = service.Process(request);

        Assert.That(result.PolicyStatus, Is.EqualTo(PolicyStatusValue.Bound));
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
