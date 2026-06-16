using System.Net;
using InsuranceIntegration.Api.IntegrationTests.Builders;
using InsuranceIntegration.Api.IntegrationTests.Infrastructure;

namespace InsuranceIntegration.Api.IntegrationTests.Api;

/// <summary>
/// End-to-end coverage for the asynchronous ingest gateway (<c>POST /api/v1/ingest</c>). The gateway
/// is idempotent on the envelope id, persists the resulting snapshots, and supports replay via
/// <c>GET /api/v1/ingest/{source}/{envelopeId}</c>.
/// </summary>
public sealed class IngestEndpointsTests : ApiTestBase
{
    [Test]
    public async Task Ingest_AcceptsAQuoteForgeEnvelopeAndReturnsAReceipt()
    {
        var envelope = new QuoteForgeEnvelopeBuilder()
            .WithEnvelopeId("qf-ingest-001")
            .WithQuoteReference("QT-INGEST-001")
            .Build();

        using var response = await PostAsync("/api/v1/ingest", envelope);

        var receipt = await response.ShouldReturnJsonAsync();
        Assert.Multiple(() =>
        {
            Assert.That(receipt.GetProperty("source").GetString(), Is.EqualTo("QUOTEFORGE"));
            Assert.That(receipt.GetProperty("envelopeId").GetString(), Is.EqualTo("qf-ingest-001"));
            Assert.That(receipt.GetProperty("messageType").GetString(), Is.EqualTo("QuoteRequest"));
        });
    }

    [Test]
    public async Task Ingest_PersistsAQuoteSnapshotThatCanBeReadBack()
    {
        var envelope = new QuoteForgeEnvelopeBuilder()
            .WithEnvelopeId("qf-ingest-002")
            .WithQuoteReference("QT-INGEST-002")
            .Build();

        using var ingestResponse = await PostAsync("/api/v1/ingest", envelope);
        ingestResponse.ShouldHaveStatus(HttpStatusCode.OK);

        using var listResponse = await GetAsync("/api/v1/quotes?take=100");
        var body = await listResponse.ShouldReturnJsonAsync();
        Assert.That(body.GetProperty("count").GetInt32(), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task Ingest_IsIdempotentOnTheEnvelopeId()
    {
        var envelope = new QuoteForgeEnvelopeBuilder()
            .WithEnvelopeId("qf-ingest-003")
            .WithQuoteReference("QT-INGEST-003")
            .Build();

        using var first = await PostAsync("/api/v1/ingest", envelope);
        var firstReceipt = await first.ShouldReturnJsonAsync();

        using var second = await PostAsync("/api/v1/ingest", envelope);
        var secondReceipt = await second.ShouldReturnJsonAsync();

        Assert.That(
            secondReceipt.GetProperty("receivedAtUtc").GetString(),
            Is.EqualTo(firstReceipt.GetProperty("receivedAtUtc").GetString()),
            "A replayed envelope must return the originally stored receipt.");
    }

    [Test]
    public async Task Ingest_ReplayGet_ReturnsTheStoredReceipt()
    {
        var envelope = new QuoteForgeEnvelopeBuilder()
            .WithEnvelopeId("qf-ingest-004")
            .WithQuoteReference("QT-INGEST-004")
            .Build();

        using var ingestResponse = await PostAsync("/api/v1/ingest", envelope);
        ingestResponse.ShouldHaveStatus(HttpStatusCode.OK);

        using var replay = await GetAsync("/api/v1/ingest/QUOTEFORGE/qf-ingest-004");

        var receipt = await replay.ShouldReturnJsonAsync();
        Assert.That(receipt.GetProperty("envelopeId").GetString(), Is.EqualTo("qf-ingest-004"));
    }

    [Test]
    public async Task Ingest_ReplayGet_Returns404_ForAnUnknownEnvelope()
    {
        using var replay = await GetAsync("/api/v1/ingest/QUOTEFORGE/does-not-exist");

        replay.ShouldHaveStatus(HttpStatusCode.NotFound);
    }
}
