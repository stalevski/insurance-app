using InsuranceIntegration.Api.CanonicalContracts.Risks;
using InsuranceIntegration.Api.Mappers.Risks;
using InsuranceIntegration.Api.Responses.Risks;
using InsuranceIntegration.Api.Services.Clearance;
using InsuranceIntegration.Api.Services.Flows;
using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.Services.Matching;
using InsuranceIntegration.Api.Services.Orchestration;
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

        Assert.That(handler.CanHandle(CreateEnvelope(type)), Is.True);
    }

    [Test]
    public void CanHandle_ReturnsFalseForUnsupportedType()
    {
        var handler = CreateHandler();

        Assert.That(handler.CanHandle(CreateEnvelope("ClaimNotice")), Is.False);
    }

    [Test]
    public async Task HandleAsync_ProducesFinalRiskResponseForPolarisRiskSubmission()
    {
        var handler = CreateHandler();

        var envelope = new SourceIngestEnvelope
        {
            Id = "evt-1",
            Source = "POLARIS_UW",
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

        var result = await handler.HandleAsync(envelope);

        Assert.That(result, Is.TypeOf<FinalRiskResponse>());
        var finalResponse = (FinalRiskResponse)result;
        Assert.That(finalResponse.ExternalReference, Is.EqualTo("Q-100045"));
        Assert.That(finalResponse.SourceSystem, Is.EqualTo("POLARIS_UW"));
    }

    private static RiskIngestHandler CreateHandler()
    {
        var calculator = new LevenshteinDistanceCalculator();
        var registry = new InMemorySubmissionRegistry();
        var clearanceService = new SubmissionClearanceService(registry, calculator);
        var riskFlowService = new RiskFlowService(clearanceService, registry);
        var riskIngestMapper = new RiskIngestMapper([new PolarisRiskMapper(), new QuoteForgeRiskMapper(), new BindPointRiskMapper()]);
        return new RiskIngestHandler(riskIngestMapper, new StubOrchestrator(riskFlowService), TimeProvider.System);
    }

    private sealed class StubOrchestrator : IRiskSubmissionOrchestrator
    {
        private readonly IRiskFlowService _riskFlowService;

        public StubOrchestrator(IRiskFlowService riskFlowService)
        {
            _riskFlowService = riskFlowService;
        }

        public Task<FinalRiskResponse> HandleAsync(
            CanonicalRiskRequest request,
            Services.Ingest.IngestContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_riskFlowService.Process(request));
        }
    }

    private static SourceIngestEnvelope CreateEnvelope(string type)
    {
        return new SourceIngestEnvelope
        {
            Id = "evt-1",
            Source = "POLARIS_UW",
            Type = type,
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc),
            Data = JsonSerializer.SerializeToElement(new { quoteId = "Q-1" })
        };
    }
}
