using System.Text.Json;
using InsuranceIntegration.Api.Persistence;
using InsuranceIntegration.Api.Services.Outbox;
using Microsoft.Extensions.Logging.Abstractions;

namespace InsuranceIntegration.Api.Tests.Outbox;

public sealed class FileOutboxPublisherTests : IDisposable
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"outbox-test-{Guid.NewGuid():N}.jsonl");

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Test]
    public async Task PublishAsync_AppendsOneJsonLinePerEvent()
    {
        var options = new OutboxOptions { Transport = OutboxTransport.File, FilePath = _filePath };
        var publisher = new FileOutboxPublisher(options, NullLogger<FileOutboxPublisher>.Instance);

        await publisher.PublishAsync(CreateMessage("PolicyBound"));
        await publisher.PublishAsync(CreateMessage("PolicyCancelled"));

        var lines = await File.ReadAllLinesAsync(_filePath);
        Assert.That(lines, Has.Length.EqualTo(2));

        var first = JsonSerializer.Deserialize<OutboxEventEnvelope>(lines[0], Web);
        var second = JsonSerializer.Deserialize<OutboxEventEnvelope>(lines[1], Web);
        Assert.Multiple(() =>
        {
            Assert.That(first!.EventType, Is.EqualTo("PolicyBound"));
            Assert.That(second!.EventType, Is.EqualTo("PolicyCancelled"));
        });
    }

    [Test]
    public void PublishAsync_WithNullMessage_Throws()
    {
        var options = new OutboxOptions { FilePath = _filePath };
        var publisher = new FileOutboxPublisher(options, NullLogger<FileOutboxPublisher>.Instance);

        Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync(null!));
    }

    private static OutboxMessageEntity CreateMessage(string eventType) => new()
    {
        EventId = Guid.CreateVersion7(),
        AggregateType = "Policy",
        AggregateId = Guid.CreateVersion7(),
        EventType = eventType,
        PayloadJson = "{}",
        OccurredAtUtc = DateTime.UtcNow
    };
}
