using System.Net.Http.Json;
using System.Text.Json;
using InsuranceIntegration.Api.Persistence;

namespace InsuranceIntegration.Api.Services.Outbox;

/// <summary>
/// Real transport: POSTs each outbox event to a configured HTTP endpoint (webhook) as JSON. Uses the
/// built-in <see cref="HttpClient"/> only — no message-broker dependency. A non-success response
/// throws so the dispatcher records the error and retries on a later poll.
/// </summary>
public sealed class WebhookOutboxPublisher : IOutboxPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OutboxOptions _options;
    private readonly ILogger<WebhookOutboxPublisher> _logger;

    public WebhookOutboxPublisher(HttpClient httpClient, OutboxOptions options, ILogger<WebhookOutboxPublisher> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxMessageEntity message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            throw new InvalidOperationException(
                "Outbox webhook transport requires 'Outbox:WebhookUrl' to be configured.");
        }

        var envelope = OutboxEventEnvelope.FromMessage(message);

        using var response = await _httpClient.PostAsJsonAsync(
            _options.WebhookUrl,
            envelope,
            SerializerOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Posted outbox event {EventType} for {AggregateType} {AggregateId} (EventId={EventId}) to {WebhookUrl} ({StatusCode}).",
                message.EventType,
                message.AggregateType,
                message.AggregateId,
                message.EventId,
                _options.WebhookUrl,
                (int)response.StatusCode);
        }
    }
}
