using System.Text.Json;
using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Real, infrastructure-free transport: appends each outbox event as a single JSON line to a local
/// file (JSON Lines). Useful for durable local delivery, debugging, or feeding a file-tailing
/// consumer. Throws on write failure so the dispatcher records the error and retries.
/// </summary>
public sealed class FileOutboxPublisher : IOutboxPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly OutboxOptions _options;
    private readonly ILogger<FileOutboxPublisher> _logger;

    public FileOutboxPublisher(OutboxOptions options, ILogger<FileOutboxPublisher> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var envelope = OutboxEventEnvelope.FromMessage(message);
        var line = JsonSerializer.Serialize(envelope, SerializerOptions) + Environment.NewLine;

        var fullPath = Path.GetFullPath(_options.FilePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.AppendAllTextAsync(fullPath, line, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Appended outbox event {EventType} for {AggregateType} {AggregateId} (EventId={EventId}) to {FilePath}.",
                message.EventType,
                message.AggregateType,
                message.AggregateId,
                message.EventId,
                fullPath);
        }
    }
}
