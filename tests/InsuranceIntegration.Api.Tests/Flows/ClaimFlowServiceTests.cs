using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class ClaimFlowServiceTests
{
    [TestCase(1000, "Low", "AutoProcess")]
    [TestCase(10000, "Medium", "ManualReview")]
    [TestCase(75000, "High", "SuspectedFraud")]
    public void Process_ResolvesSeverityAndTriage(decimal incurred, string expectedSeverity, string expectedTriage)
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-1",
            PolicyReference = "POL-1",
            ClaimantName = "Avery Cole",
            SourceSystem = "CLAIMFORGE",
            IncurredAmount = incurred,
            ReservedAmount = 0m,
            PaidAmount = 0m
        };

        var result = service.Process(request);

        Assert.That(result.Severity, Is.EqualTo(expectedSeverity));
        Assert.That(result.TriageDecision, Is.EqualTo(expectedTriage));
    }

    [Test]
    public void Process_AutoClosesFullyPaidLowSeverityClaim()
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-1",
            PolicyReference = "POL-1",
            ClaimantName = "Avery Cole",
            SourceSystem = "CLAIMFORGE",
            IncurredAmount = 400m,
            ReservedAmount = 400m,
            PaidAmount = 400m
        };

        var result = service.Process(request);

        Assert.That(result.AutoClosed, Is.True);
        Assert.That(result.FinalStatus, Is.EqualTo("Closed"));
    }
}
