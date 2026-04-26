using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Snapshots;

namespace InsuranceIntegration.Api.Tests.Snapshots;

public sealed class PolicySnapshotProjectorTests
{
    [Test]
    public void Apply_CreatesNewSnapshotFromFirstEvent()
    {
        var projector = new PolicySnapshotProjector();
        var (request, response, context) = BuildBindEvent(
            source: "BINDPOINT",
            envelopeId: "evt-bp-1",
            policyReference: "POL-7781",
            quoteReference: "QT-2201",
            insuredName: "Northwind Storage Ltd",
            basePremium: 12500m,
            adjustedPremium: 13750m);

        var snapshot = projector.Apply(current: null, request, response, context);

        Assert.That(snapshot.PolicyReference, Is.EqualTo("POL-7781"));
        Assert.That(snapshot.QuoteReference, Is.EqualTo("QT-2201"));
        Assert.That(snapshot.Insured.Name, Is.EqualTo("Northwind Storage Ltd"));
        Assert.That(snapshot.Premium.Base, Is.EqualTo(12500m));
        Assert.That(snapshot.Premium.Adjusted, Is.EqualTo(13750m));
        Assert.That(snapshot.History, Has.Count.EqualTo(1));
        Assert.That(snapshot.History[0].Source, Is.EqualTo("BINDPOINT"));
        Assert.That(snapshot.History[0].EnvelopeId, Is.EqualTo("evt-bp-1"));
        Assert.That(snapshot.ExternalReferences["BINDPOINT"], Is.EqualTo("POL-7781"));
        Assert.That(snapshot.LastUpdatedUtc, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void Apply_MergesSubsequentEventPreservingExistingFields()
    {
        var projector = new PolicySnapshotProjector();

        var (firstRequest, firstResponse, firstContext) = BuildBindEvent(
            source: "BINDPOINT",
            envelopeId: "evt-bp-1",
            policyReference: "POL-7781",
            quoteReference: "QT-2201",
            insuredName: "Northwind Storage Ltd",
            basePremium: 12500m,
            adjustedPremium: 13750m);
        var snapshot = projector.Apply(null, firstRequest, firstResponse, firstContext);

        // Second event: an endorsement adds new info but not all fields are present.
        var (secondRequest, secondResponse, secondContext) = BuildBindEvent(
            source: "POLARIS_UW",
            envelopeId: "evt-polaris-2",
            policyReference: "POL-7781",
            quoteReference: null,
            insuredName: null,
            basePremium: 0m,
            adjustedPremium: 0m,
            externalReference: "Q-100045-ENDORSEMENT");
        var merged = projector.Apply(snapshot, secondRequest, secondResponse, secondContext);

        Assert.That(merged.PolicyReference, Is.EqualTo("POL-7781"));
        Assert.That(merged.QuoteReference, Is.EqualTo("QT-2201"), "QuoteReference must be preserved when second event omits it");
        Assert.That(merged.Insured.Name, Is.EqualTo("Northwind Storage Ltd"), "Insured name preserved when second event omits it");
        Assert.That(merged.Premium.Base, Is.EqualTo(12500m), "Premium preserved when second event lacks it");
        Assert.That(merged.History, Has.Count.EqualTo(2));
        Assert.That(merged.History[0].EnvelopeId, Is.EqualTo("evt-bp-1"));
        Assert.That(merged.History[1].EnvelopeId, Is.EqualTo("evt-polaris-2"));
        Assert.That(merged.ExternalReferences["BINDPOINT"], Is.EqualTo("POL-7781"));
        Assert.That(merged.ExternalReferences["POLARIS_UW"], Is.EqualTo("Q-100045-ENDORSEMENT"));
    }

    [Test]
    public void Apply_DoesNotWipeCoverageWhenSecondEventHasNoSections()
    {
        var projector = new PolicySnapshotProjector();

        var (firstRequest, firstResponse, firstContext) = BuildBindEvent(
            source: "BINDPOINT",
            envelopeId: "evt-bp-1",
            policyReference: "POL-7781",
            quoteReference: "QT-2201",
            insuredName: "Northwind Storage Ltd",
            basePremium: 12500m,
            adjustedPremium: 13750m,
            sectionCount: 3,
            totalSumInsured: 5_000_000m,
            warnings: new[] { "Sum insured high" });
        var snapshot = projector.Apply(null, firstRequest, firstResponse, firstContext);

        Assert.That(snapshot.Coverage.SectionCount, Is.EqualTo(3));
        Assert.That(snapshot.Coverage.Warnings, Has.Count.EqualTo(1));

        var (request2, response2, context2) = BuildBindEvent(
            source: "POLARIS_UW",
            envelopeId: "evt-polaris-2",
            policyReference: "POL-7781",
            quoteReference: null,
            insuredName: null,
            basePremium: 0m,
            adjustedPremium: 0m,
            sectionCount: 0,
            totalSumInsured: 0m,
            warnings: Array.Empty<string>());
        var merged = projector.Apply(snapshot, request2, response2, context2);

        Assert.That(merged.Coverage.SectionCount, Is.EqualTo(3), "Coverage should be preserved when new event has no sections");
        Assert.That(merged.Coverage.TotalSumInsured, Is.EqualTo(5_000_000m));
        Assert.That(merged.Coverage.Warnings, Has.Count.EqualTo(1));
    }

    private static (CanonicalRiskRequest, FinalRiskResponse, IngestContext) BuildBindEvent(
        string source,
        string envelopeId,
        string policyReference,
        string? quoteReference,
        string? insuredName,
        decimal basePremium,
        decimal adjustedPremium,
        int sectionCount = 0,
        decimal totalSumInsured = 0m,
        IEnumerable<string>? warnings = null,
        string? externalReference = null)
    {
        var request = new CanonicalRiskRequest
        {
            ExternalReference = externalReference ?? policyReference,
            ProductCode = "COMMERCIAL_PROPERTY",
            SourceSystem = source,
            TransactionType = "PolicyBind",
            CurrencyCode = "USD",
            Insured = new InsuredData { FullName = insuredName },
            Quote = new QuoteData { QuoteReference = quoteReference, EffectiveDate = new DateOnly(2026, 5, 1), ExpiryDate = new DateOnly(2027, 4, 30) },
            Policy = new PolicyData { PolicyReference = policyReference }
        };

        var response = new FinalRiskResponse
        {
            EntityId = Guid.NewGuid(),
            ExternalReference = externalReference ?? policyReference,
            ProductCode = "COMMERCIAL_PROPERTY",
            SourceSystem = source,
            TransactionType = "PolicyBind",
            SubmissionStatus = "Received",
            QuoteStatus = QuoteStatusValue.Quoted,
            PolicyStatus = PolicyStatusValue.Bound,
            ClearanceDecision = "AutoCleared",
            AutoCleared = true,
            FinalStatus = "ReadyForDownstreamDispatch",
            BasePremium = basePremium,
            AdjustedPremium = adjustedPremium,
            SectionCount = sectionCount,
            TotalSumInsured = totalSumInsured,
            CoverageWarnings = warnings?.ToList() ?? new List<string>()
        };

        var context = new IngestContext
        {
            Source = source,
            EnvelopeId = envelopeId,
            MessageType = "PolicyBindRequest",
            ReceivedAtUtc = DateTime.UtcNow
        };

        return (request, response, context);
    }
}
