using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Tests.Flows;

namespace InsuranceIntegration.Api.Tests.Clearance;

public sealed class SubmissionClearanceServiceTests
{
    private static SubmissionClearanceService CreateService(ISubmissionRegistry registry)
    {
        return new SubmissionClearanceService(registry, new LevenshteinDistanceCalculator());
    }

    [Test]
    public void Evaluate_ReturnsClearedWhenNoKnownSubmissions()
    {
        var registry = new InMemorySubmissionRegistry();
        var service = CreateService(registry);
        var request = TestRiskRequestFactory.Create(underwritingYear: 2026);

        var result = service.Evaluate(request);

        Assert.That(result.Outcome, Is.EqualTo(SubmissionClearanceOutcome.Cleared));
        Assert.That(result.IsCleared, Is.True);
    }

    [Test]
    public void Evaluate_DetectsDuplicateWhenInsuredMatchesWithinTolerance()
    {
        var registry = new InMemorySubmissionRegistry();
        registry.Register(new KnownSubmissionRecord
        {
            ExternalReference = "EXT-PRIOR",
            InsuredName = "Northwind Storage Ltd",
            ProductCode = "COMMERCIAL_PROPERTY",
            UnderwritingYear = 2026,
            BrokerCode = "BRK-1"
        });

        var service = CreateService(registry);
        var request = TestRiskRequestFactory.Create(
            productCode: "COMMERCIAL_PROPERTY",
            underwritingYear: 2026,
            insuredName: "Northwind Storage Ltd",
            brokerCode: "BRK-1");

        var result = service.Evaluate(request);

        Assert.That(result.Outcome, Is.EqualTo(SubmissionClearanceOutcome.DuplicateSubmission));
        Assert.That(result.IsCleared, Is.False);
        Assert.That(result.DuplicateExternalReference, Is.EqualTo("EXT-PRIOR"));
    }

    [Test]
    public void Evaluate_DetectsConflictingBrokerAgainstExistingSubmission()
    {
        var registry = new InMemorySubmissionRegistry();
        registry.Register(new KnownSubmissionRecord
        {
            ExternalReference = "EXT-PRIOR",
            InsuredName = "Northwind Storage Ltd",
            ProductCode = "COMMERCIAL_PROPERTY",
            UnderwritingYear = 2026,
            BrokerCode = "BRK-OTHER"
        });

        var service = CreateService(registry);
        var request = TestRiskRequestFactory.Create(
            productCode: "COMMERCIAL_PROPERTY",
            underwritingYear: 2026,
            insuredName: "Northwind Storage Ltd",
            brokerCode: "BRK-1");

        var result = service.Evaluate(request);

        Assert.That(result.Outcome, Is.EqualTo(SubmissionClearanceOutcome.ConflictingBroker));
        Assert.That(result.IsCleared, Is.False);
    }
}
