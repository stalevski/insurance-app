using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Outbox;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class OutboxEventEnvelopeTests
{
    [Test]
    public void FromMessage_CopiesFieldsAndParsesPayload()
    {
        var eventId = Guid.CreateVersion7();
        var aggregateId = Guid.CreateVersion7();
        var correlationId = Guid.CreateVersion7();
        var occurred = new DateTime(2026, 6, 13, 8, 30, 0, DateTimeKind.Utc);
        var message = new OutboxMessageEntity
        {
            EventId = eventId,
            AggregateType = "Policy",
            AggregateId = aggregateId,
            EventType = "PolicyCancelled",
            PayloadJson = "{\"reference\":\"POL-1\",\"premium\":1200.50}",
            CorrelationId = correlationId,
            OccurredAtUtc = occurred
        };

        var envelope = OutboxEventEnvelope.FromMessage(message);

        Assert.Multiple(() =>
        {
            Assert.That(envelope.EventId, Is.EqualTo(eventId));
            Assert.That(envelope.AggregateType, Is.EqualTo("Policy"));
            Assert.That(envelope.AggregateId, Is.EqualTo(aggregateId));
            Assert.That(envelope.EventType, Is.EqualTo("PolicyCancelled"));
            Assert.That(envelope.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(envelope.OccurredAtUtc, Is.EqualTo(occurred));
            Assert.That(envelope.Payload, Is.Not.Null);
            Assert.That(envelope.Payload!.Value.GetProperty("reference").GetString(), Is.EqualTo("POL-1"));
        });
    }

    [Test]
    public void FromMessage_WithBlankPayload_LeavesPayloadNull()
    {
        var message = new OutboxMessageEntity
        {
            EventId = Guid.CreateVersion7(),
            AggregateType = "Policy",
            AggregateId = Guid.CreateVersion7(),
            EventType = "PolicyBound",
            PayloadJson = "",
            OccurredAtUtc = DateTime.UtcNow
        };

        var envelope = OutboxEventEnvelope.FromMessage(message);

        Assert.That(envelope.Payload, Is.Null);
    }

    [Test]
    public void FromMessage_WithNullMessage_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OutboxEventEnvelope.FromMessage(null!));
    }
}
