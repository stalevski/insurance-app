using InsuranceIntegration.Api.CanonicalContracts.Claims;
using InsuranceIntegration.Api.Services.Flows;

namespace InsuranceIntegration.Api.Tests.Flows;

public sealed class ClaimFlowIndemnityTests
{
    [Test]
    public void Process_SubtractsDeductibleFromIncurredToProduceIndemnity()
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-IND-1",
            PolicyReference = "POL-1",
            ClaimantName = "Sample Insured",
            SourceSystem = "TEST",
            IncurredAmount = 12_000m,
            ReservedAmount = 12_000m,
            PaidAmount = 0m,
            DeductibleApplied = 2_500m
        };

        var result = service.Process(request);

        Assert.That(result.DeductibleAmount, Is.EqualTo(2_500m));
        Assert.That(result.IndemnityAmount, Is.EqualTo(9_500m));
        Assert.That(result.LimitBreached, Is.False);
    }

    [Test]
    public void Process_CapsIndemnityAtPerOccurrenceLimitAndFlagsLimitBreached()
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-IND-2",
            PolicyReference = "POL-1",
            ClaimantName = "Sample Insured",
            SourceSystem = "TEST",
            IncurredAmount = 60_000m,
            DeductibleApplied = 0m,
            PerOccurrenceLimit = 25_000m
        };

        var result = service.Process(request);

        Assert.That(result.IndemnityAmount, Is.EqualTo(25_000m));
        Assert.That(result.LimitBreached, Is.True);
    }

    [Test]
    public void Process_DeductibleNeverExceedsIncurredAmount()
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-IND-3",
            PolicyReference = "POL-1",
            ClaimantName = "Sample Insured",
            SourceSystem = "TEST",
            IncurredAmount = 500m,
            DeductibleApplied = 1000m
        };

        var result = service.Process(request);

        Assert.That(result.DeductibleAmount, Is.EqualTo(500m));
        Assert.That(result.IndemnityAmount, Is.EqualTo(0m));
    }

    [Test]
    public void Process_PassesThroughAffectedCoordinates()
    {
        var service = new ClaimFlowService();
        var request = new CanonicalClaimRequest
        {
            ClaimReference = "CLM-IND-4",
            PolicyReference = "POL-1",
            ClaimantName = "Sample Insured",
            SourceSystem = "TEST",
            IncurredAmount = 1_500m,
            AffectedSectionCode = "PROP",
            AffectedSubcoverCode = "FIRE",
            AffectedPerilCode = "FIRE"
        };

        var result = service.Process(request);

        Assert.That(result.AffectedSectionCode, Is.EqualTo("PROP"));
        Assert.That(result.AffectedSubcoverCode, Is.EqualTo("FIRE"));
        Assert.That(result.AffectedPerilCode, Is.EqualTo("FIRE"));
    }
}
