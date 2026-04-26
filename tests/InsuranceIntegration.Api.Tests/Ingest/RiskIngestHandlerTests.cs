using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Snapshots;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Ingest;

public sealed class RiskIngestHandlerTests
{
    [TestCase("RiskSubmission")]
    [TestCase("QuoteRequest")]
    [TestCase("PolicyBindRequest")]
    public void CanHandle_ReturnsTrueForSupportedTypes(string type)
    {
        var handler = CreateHandler();

        var envelope = CreateEnvelope(type);

        Assert.That(handler.CanHandle(envelope), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsFalseForUnsupportedType()
    {
        var handler = CreateHandler();

        var envelope = CreateEnvelope("ClaimNotice");

        Assert.That(handler.CanHandle(envelope), Is.False);
    }

    [Test]
    public void Handle_ProducesFinalRiskResponseForContosoRiskSubmission()
    {
        var handler = CreateHandler();

        var envelope = new SourceIngestEnvelope
        {
            Id = "evt-1",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new
            {
                quoteId = "Q-100045",
                insuredName = "Northwind Storage Ltd",
                trade = "CommercialProperty",
                estimatedPremium = 12500m
            })
        };

        var result = handler.Handle(envelope);

        Assert.That(result, Is.TypeOf<FinalRiskResponse>());
        var finalResponse = (FinalRiskResponse)result;
        Assert.That(finalResponse.ExternalReference, Is.EqualTo("Q-100045"));
        Assert.That(finalResponse.SourceSystem, Is.EqualTo("CONTOSO_UW"));
    }

    private static RiskIngestHandler CreateHandler()
    {
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new InMemorySubmissionRegistry();
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var riskFlowService = new RiskFlowService(clearanceService, registry);
        var riskIngestMapper = new RiskIngestMapper([new ContosoRiskMapper(), new QuoteForgeRiskMapper(), new BindPointRiskMapper()]);
        return new RiskIngestHandler(riskIngestMapper, riskFlowService, new NoOpRiskSnapshotRouter(), TimeProvider.System);
    }

    private sealed class NoOpRiskSnapshotRouter : IRiskSnapshotRouter
    {
        public void Route(CanonicalRiskRequest request, FinalRiskResponse response, IngestContext context)
        {
        }
    }

    private static SourceIngestEnvelope CreateEnvelope(string type)
    {
        return new SourceIngestEnvelope
        {
            Id = "evt-1",
            Source = "CONTOSO_UW",
            Type = type,
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new { quoteId = "Q-1" })
        };
    }
}
