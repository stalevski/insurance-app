using InsuranceIntegration.Api.IntegrationTests.Builders;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;
using InsuranceIntegration.Api.SourceContracts.Ingest;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// Coverage for the synchronous risk-processing surface: the canonical entry point
/// (<c>POST /api/v1/risks</c>) and the source-shaped entry point (<c>POST /api/v1/ingest/risks</c>).
/// Both run the pure risk flow and echo a <c>FinalRiskResponse</c> without persisting state.
/// </summary>
public sealed class RiskEndpointsTests : ApiTestBase
{
    [Test]
    public async Task ProcessRisk_EchoesTheCanonicalSubmission()
    {
        var request = new CanonicalRiskRequestBuilder()
            .WithExternalReference("EXT-RISK-100")
            .WithProductCode("COMMERCIAL_PROPERTY")
            .Build();

        using var response = await PostAsync("/api/v1/risks", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetProperty("externalReference").GetString(), Is.EqualTo("EXT-RISK-100"));
            Assert.That(body.GetProperty("productCode").GetString(), Is.EqualTo("COMMERCIAL_PROPERTY"));
            Assert.That(body.GetProperty("claimCount").GetInt32(), Is.EqualTo(1));
            Assert.That(body.GetProperty("finalStatus").GetString(), Is.Not.Empty);
        });
    }

    [Test]
    public async Task ProcessRisk_CountsEveryClaimOnTheSubmission()
    {
        var request = new CanonicalRiskRequestBuilder()
            .WithExternalReference("EXT-RISK-CLAIMS")
            .WithClaims(3, incurredPerClaim: 1_200m, reservedPerClaim: 300m)
            .Build();

        using var response = await PostAsync("/api/v1/risks", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("claimCount").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public async Task ProcessRisk_HonoursTheRequestedProductLine()
    {
        var request = new CanonicalRiskRequestBuilder()
            .WithExternalReference("EXT-RISK-LIAB")
            .WithProductCode("LIABILITY")
            .Build();

        using var response = await PostAsync("/api/v1/risks", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("productCode").GetString(), Is.EqualTo("LIABILITY"));
    }

    [Test]
    public async Task ProcessSourceRisk_MapsAQuoteForgePayloadToACanonicalResponse()
    {
        var envelope = new QuoteForgeEnvelopeBuilder()
            .WithQuoteReference("QT-RISK-INGEST")
            .Build();
        var request = new SourceIngestRequest
        {
            SourceSystem = envelope.Source,
            MessageType = envelope.Type,
            Payload = envelope.Data,
        };

        using var response = await PostAsync("/api/v1/ingest/risks", request);

        var body = await response.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("externalReference").GetString(), Is.EqualTo("QT-RISK-INGEST"));
    }
}
