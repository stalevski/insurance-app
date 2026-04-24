using InsuranceIntegration.Api.Services.Ingest;
using InsuranceIntegration.Api.SourceContracts.Ingest;
using System.Text.Json;

namespace InsuranceIntegration.Api.Tests.Ingest;

public sealed class IdempotencyDispatchTests
{
    [Test]
    public void Dispatch_ReturnsStoredResultWhenEnvelopeIdAlreadyProcessed()
    {
        var firstResultPayload = new { status = "first" };
        var secondResultPayload = new { status = "second" };

        var counter = new CountingStubHandler("Counting", firstResultPayload);
        var store = new InMemoryIdempotencyStore();
        var dispatcher = new IngestDispatcher([counter], store);

        var envelope = new SourceIngestEnvelope
        {
            Id = "evt-idem-1",
            Source = "TEST_SOURCE",
            Type = "RiskSubmission",
            SchemaVersion = "1.0",
            OccurredAtUtc = DateTime.UtcNow,
            Data = JsonSerializer.SerializeToElement(new { hello = "world" })
        };

        var first = dispatcher.Dispatch(envelope);

        counter.SetResult(secondResultPayload);
        var second = dispatcher.Dispatch(envelope);

        Assert.That(counter.HandleCount, Is.EqualTo(1));
        Assert.That(second.Result, Is.SameAs(first.Result));
    }

    private sealed class CountingStubHandler : IIngestHandler
    {
        private object _result;

        public CountingStubHandler(string name, object result)
        {
            Name = name;
            _result = result;
        }

        public string Name { get; }

        public int HandleCount { get; private set; }

        public bool CanHandle(SourceIngestEnvelope envelope)
        {
            return true;
        }

        public object Handle(SourceIngestEnvelope envelope)
        {
            HandleCount++;
            return _result;
        }

        public void SetResult(object result)
        {
            _result = result;
        }
    }
}
