using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Ingest;

public sealed class IngestDispatcherTests
{
    [Test]
    public void Dispatch_SelectsFirstCompatibleHandlerAndReturnsEnvelopeMetadata()
    {
        var unsupportedHandler = new StubIngestHandler("UnsupportedHandler", canHandle: false, result: null);
        var supportedHandler = new StubIngestHandler("SupportedHandler", canHandle: true, result: new { status = "ok" });
        var dispatcher = new IngestDispatcher([unsupportedHandler, supportedHandler], new InMemoryIdempotencyStore());

        var envelope = CreateEnvelope();

        var response = dispatcher.Dispatch(envelope);

        Assert.That(response.EnvelopeId, Is.EqualTo("evt-1"));
        Assert.That(response.Source, Is.EqualTo("CONTOSO_UW"));
        Assert.That(response.Type, Is.EqualTo("RiskSubmission"));
        Assert.That(response.HandlerName, Is.EqualTo("SupportedHandler"));
        Assert.That(response.CorrelationId, Is.EqualTo("corr-1"));
    }

    [Test]
    public void Dispatch_ThrowsWhenNoHandlerCanHandleEnvelope()
    {
        var dispatcher = new IngestDispatcher([new StubIngestHandler("None", canHandle: false, result: null)], new InMemoryIdempotencyStore());

        var envelope = CreateEnvelope();

        var exception = Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(envelope));
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("No ingest handler registered"));
    }

    private static SourceIngestEnvelope CreateEnvelope()
    {
        return new SourceIngestEnvelope
        {
            Id = "evt-1",
            Source = "CONTOSO_UW",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = new DateTime(2026, 4, 24, 8, 0, 0, DateTimeKind.Utc),
            CorrelationId = "corr-1",
            Data = JsonSerializer.SerializeToElement(new { quoteId = "Q-1" })
        };
    }
}
